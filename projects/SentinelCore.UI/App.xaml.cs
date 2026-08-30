// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         App.xaml.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts;
using SentinelCore.Infrastructure.DependencyInjection;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;




namespace SentinelCore.UI;


/// <summary>
///     Composition root for the SentinelCore.UI application.
///     Wires the <see cref="IHost" /> container, registers all services,
///     and manages application lifecycle including graceful shutdown.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    /// <summary>
    ///     Tracks whether the <see cref="IHost" /> has been started.
    /// </summary>
    private bool _hostStarted;

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






    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
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
        services.AddSentinelCore(sentinelSettings);

        // UI layer — services, ViewModels, Views, and navigation
        services.AddSentinelCoreUI();

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
    }






    /// <summary>
    ///     Resolves a service from the DI container.
    /// </summary>
    public T? GetService<T>() where T : class
    {
        return _host?.Services.GetService(typeof(T)) as T;
    }






    /// <summary>
    ///     Cancels the shared <see cref="ShutdownToken" />, stops the <see cref="IHost" />,
    ///     and disposes resources. Safe to call from any exit path.
    /// </summary>
    private async Task InitiateShutdownAsync()
    {
        if (!_shutdownCts.IsCancellationRequested)
        {
            await _shutdownCts.CancelAsync();
        }

        if (_host is not null)
        {
            try
            {
                await _host.StopAsync(ShutdownToken).ConfigureAwait(false);
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






    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
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






    private async void OnExit(object? sender, ExitEventArgs? e)
    {
        await InitiateShutdownAsync();
    }






    private async void OnStartup(object? sender, StartupEventArgs e)
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
                await _shutdownCts.CancelAsync();
            }

            MessageBox.Show($"A critical error occurred during startup:\n\n{ex.Message}\n\nSee the debug output for details.", "SentinelCore — Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            this.Shutdown(1);
        }
    }






    private async Task StartApplicationAsync(StartupEventArgs e)
    {
        // Startup breadcrumb — visible in the VS Output window.
        System.Diagnostics.Debug.WriteLine("Starting host…");

        string? appLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);

        _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c => { c.SetBasePath(appLocation ?? string.Empty); })
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    // WinExe has no attached console — Debug output surfaces in the VS Output window.
                    logging.AddDebug();
                    logging.AddJsonConsole(options => { options.JsonWriterOptions = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }; });
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

        await StartHostOnceAsync();
    }






    /// <summary>
    ///     Starts the <see cref="IHost" /> exactly once.
    /// </summary>
    private async Task StartHostOnceAsync(CancellationToken cancellationToken = default)
    {
        if (_hostStarted)
        {
            return;
        }

        Throw.IfNull(_host);

        _hostStarted = true;
        try
        {
            await _host.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _hostStarted = false;
            throw;
        }

        // Show the main window after the host is running
        Current.Dispatcher.Invoke(() =>
        {
            MainWindow = new MainWindow(_host.Services.GetRequiredService<INavigationService>());
            MainWindow.Show();
        });
    }
}