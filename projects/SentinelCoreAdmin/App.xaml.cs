// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         App.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using CommunityToolkit.WinUI.Notifications;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SentinelCore.Contracts;

using SentinelCoreAdmin.Activation;
using SentinelCoreAdmin.Models;
using SentinelCoreAdmin.Services;

using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;




namespace SentinelCoreAdmin;





// For more information about application lifecycle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview





// WPF UI elements use language en-US by default.
// If you need to support other cultures make sure you add converters and review dates and numbers in your UI to ensure everything adapts correctly.
// Tracking issue for improving this is https://github.com/dotnet/wpf/issues/1946
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    ///     Shared cancellation token source that is cancelled when the application
    ///     begins shutting down (normal exit, unhandled exception, or OS session ending).
    ///     Services can observe <see cref="ShutdownToken" /> to cooperatively cancel work.
    /// </summary>
    private CancellationTokenSource _shutdownCts = new();

    /// <summary>
    ///     A <see cref="CancellationToken" /> that is cancelled when the application is shutting down.
    ///     Thread this through long-running async operations (chat, orchestration, workflows)
    ///     so they can be cancelled gracefully on all exit paths.
    /// </summary>
    public CancellationToken ShutdownToken
    {
        get => _shutdownCts.Token;
    }








    private void ConfigureServices([NotNull] HostBuilderContext context, [NotNull] IServiceCollection services)
    {
        // App host & activation
        services.AddAppHostModule();

        // Identity & Microsoft Graph
        services.AddIdentityModule();

        // Core infrastructure services
        services.AddCoreServicesModule();

        // Navigation & window management
        services.AddNavigationModule();

        // Toast notifications
        services.AddNotificationsModule();

        // SentinelCore orchestration, events, and case-flow
        SentinelCoreSettings sentinelSettings = new()
        {
            SqlConnectionString = Environment.GetEnvironmentVariable("SENTINEL_CORE") ?? string.Empty,
            TraceEnabled = true,
            TraceLogLevel = LogLevel.Trace,
            OrchestrationType = OrchestrationType.TheCore,
            DefaultModel = new ModelProfile("http://127.0.0.1:11434", "glm-5.1:cloud", .2f, 15000, 1, .2f),
            DefaultUtilityModel = new ModelProfile("http://127.0.0.1:11434", "glm-5.1:cloud", 0.1f, 12000, 1, 0.3f)
        };
        services.AddSentinelCoreModule(sentinelSettings);

        // Views & ViewModels
        services.AddViewsAndViewModelsModule();

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
    }








    [CanBeNull]
    public T? GetService<T>() where T : class => _host?.Services.GetService(typeof(T)) as T;








    /// <summary>
    ///     Cancels the shared <see cref="ShutdownToken" />, stops the <see cref="IHost" />,
    ///     and disposes resources. Safe to call from any exit path.
    /// </summary>
    private async Task InitiateShutdownAsync()
    {
        if (!_shutdownCts.IsCancellationRequested)
        {
            _shutdownCts.Cancel();
        }

        if (_host is not null)
        {
            try
            {
                await _host.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping host: {ex}");
            }

            _host.Dispose();
            _host = null;
        }

        _shutdownCts.Dispose();
    }








    private void OnDispatcherUnhandledException([CanBeNull] object sender, [NotNull] DispatcherUnhandledExceptionEventArgs e)
    {
        ILogger<App>? logger = _host?.Services.GetService<ILogger<App>>();
        logger?.LogCritical(e.Exception, "Unhandled dispatcher exception — initiating shutdown.");

        // Signal all in-flight async work to cancel immediately.
        if (!_shutdownCts.IsCancellationRequested)
        {
            _shutdownCts.Cancel();
        }

        // Prevent the default WPF crash dialog — we want to shut down cleanly.
        e.Handled = true;

        // Begin graceful shutdown on the dispatcher so OnExit fires.
        Current.Dispatcher.InvokeAsync(() => this.Shutdown());
    }








    private async void OnExit([CanBeNull] object sender, [CanBeNull] ExitEventArgs e)
    {
        await InitiateShutdownAsync();
    }








    private void OnSessionEnding([CanBeNull] object sender, [CanBeNull] SessionEndingCancelEventArgs e)
    {
        // OS is ending the session (logoff / shutdown / restart) — cancel cooperatively.
        // We do not cancel the e.Cancel; we allow the session to end but signal in-flight work.
        InitiateShutdownAsync().GetAwaiter().GetResult();
    }








    private async void OnStartup([CanBeNull] object sender, [NotNull] StartupEventArgs e)
    {
        try
        {
            await StartApplicationAsync(e);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] Startup failed: {ex}");

            // Ensure the shutdown token is cancelled so in-flight work stops.
            if (!_shutdownCts.IsCancellationRequested)
            {
                _shutdownCts.Cancel();
            }

            MessageBox.Show($"A critical error occurred during startup:\n\n{ex.Message}\n\nSee the debug output for details.", "SentinelCore — Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            this.Shutdown(1);
        }
    }








    private async Task StartApplicationAsync(StartupEventArgs e)
    {
        // https://docs.microsoft.com/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            if (!ReferenceEquals(Current?.Dispatcher, null))
            {
                Current?.Dispatcher.Invoke(async () =>
                {
                    IConfiguration config = GetService<IConfiguration>();
                    if (config != null)
                    {
                        config[ToastNotificationActivationHandler.ActivationArguments] = toastArgs?.Argument;
                    }

                    if (!ReferenceEquals(_host, null))
                    {
                        await _host.StartAsync(ShutdownToken);
                    }
                });
            }
        };

        // TODO: Register arguments you want to use on App initialization
        Dictionary<string, string?> activationArgs = new() { { ToastNotificationActivationHandler.ActivationArguments, string.Empty } };
        string? appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        // For more information about .NET generic host see  https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0
        _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(appLocation ?? string.Empty);
                    c.AddInMemoryCollection(activationArgs);
                })
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddJsonConsole(options => { options.JsonWriterOptions = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }; });
                    logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Information);
                })
                .Build();

        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            // ToastNotificationActivator code will run after this completes and will show a window if necessary.
            return;
        }

        await _host.StartAsync();
    }
}