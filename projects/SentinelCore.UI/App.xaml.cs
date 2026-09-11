// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         App.xaml.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts;
using SentinelCore.Infrastructure.DependencyInjection;
using SentinelCore.Mcp;
using SentinelCore.RemoteKB.Persistence;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;

using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;




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
    ///     Guards <see cref="_shutdownCts" /> against double disposal when both the
    ///     unhandled-exception path and <see cref="OnExit" /> run.
    /// </summary>
    private bool _shutdownDisposed;

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
        // SentinelCore orchestration, events, and case-flow.
        // Model configuration is owned by the Model Configuration page — loaded
        // from the persisted document. There is no hardcoded fallback: agents
        // without configuration are gated at the factory.
        SentinelCoreSettings sentinelSettings = new()
        {
            SqlConnectionString = Environment.GetEnvironmentVariable("SENTINEL_CORE") ?? string.Empty,
            TraceEnabled = true,
            TraceLogLevel = LogLevel.Trace,
            OrchestrationType = OrchestrationType.TheCore
        };

        // Seed per-agent models from the persisted configuration document so the
        // first orchestration after startup uses the user's saved configuration.
        FileModelConfigStore seedStore = new(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FileModelConfigStore>.Instance);
        ModelConfigDocument? document = seedStore.Load();

        if (document is not null)
        {
            foreach (KeyValuePair<string, ModelProfile> entry in document.AgentModels)
            {
                sentinelSettings.AgentModels[entry.Key] = entry.Value;
            }
        }

        services.AddSentinelCore(sentinelSettings);

        // UI layer — services, ViewModels, Views, and navigation
        services.AddSentinelCoreUI();

        // Model configuration gate — evaluates catalog agents against the live settings.
        services.AddSingleton<IModelConfigGate>(sp => new ModelConfigGate(
            sp.GetRequiredService<ISentinelAgentCatalog>(),
            sp.GetRequiredService<IAgentProfileBuilder>(),
            sp.GetRequiredService<IOptions<SentinelCoreSettings>>().Value));

        // Application shutdown token — injected into ViewModels so in-flight work
        // can be cancelled cooperatively when the app exits. CancellationToken is a
        // struct, so the non-generic registration overload is required.
        services.AddSingleton(typeof(CancellationToken), _ => (object)ShutdownToken);

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

        // AddDbContextFactory registers BOTH IDbContextFactory<SentinelCoreDBContext> (singleton,
        // required by CaseFlowEngine) and the DbContext itself (scoped, required by
        // EvidenceStore/PatternMemoryStore/SignalRepository). AddDbContext alone would leave the
        // factory unresolvable.
        services.AddDbContextFactory<SentinelCoreDBContext>(options =>
        {
            options.UseSqlServer(Environment.GetEnvironmentVariable("SENTINEL_CORE"));
        });
        services.AddDbContextFactory<SentinelRAGDBContext>(options =>
        {
            options.UseSqlServer(Environment.GetEnvironmentVariable("REMOTEKB"));
        });



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
    ///     and disposes resources. Safe to call from any exit path and idempotent.
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
                // The shutdown token is already cancelled; stopping the host with it
                // would abort graceful hosted-service shutdown. Use a fresh timeout
                // token so IHostedService.StopAsync implementations get a chance to run.
                using CancellationTokenSource stopCts = new(TimeSpan.FromSeconds(10));
                await _host.StopAsync(stopCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping host: {ex}");
            }

            _host.Dispose();
            _host = null;
        }

        if (!_shutdownDisposed)
        {
            _shutdownDisposed = true;
            _shutdownCts.Dispose();
        }
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

        // Fail fast with a clear message when required configuration is missing;
        // otherwise EF Core surfaces a cryptic activation error much later.
        string? caseFlowConnection = Environment.GetEnvironmentVariable("SENTINEL_CORE");
        string? remoteKbConnection = Environment.GetEnvironmentVariable("REMOTEKB");

        if (string.IsNullOrWhiteSpace(caseFlowConnection) || string.IsNullOrWhiteSpace(remoteKbConnection))
        {
            throw new InvalidOperationException(
                "Required environment variables are missing. Set SENTINEL_CORE (case flow database connection string) "
                + "and REMOTEKB (remote knowledge base connection string) before starting SentinelCore.");
        }

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
