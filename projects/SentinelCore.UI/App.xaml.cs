// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         App.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Infrastructure.DependencyInjection;
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
public partial class App : Application
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

    public IServiceProvider Services
    {
        get => _host?.Services ?? throw new InvalidOperationException("The application host is not available.");
    }

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
            //TODO: Isolate Caseflow engine db configuration to make module optional. Keep seams to case flow engine clean.
            SqlConnectionString = Environment.GetEnvironmentVariable("SENTINEL_CORE") ?? string.Empty,
            TraceEnabled = true,
            TraceLogLevel = LogLevel.Trace,
            OrchestrationType = OrchestrationType.TheCore
        };

        // Seed per-agent models from the persisted configuration document so the
        // first orchestration after startup uses the user's saved configuration.
        FileModelConfigStore seedStore = new(Microsoft.Extensions.Logging.Abstractions.NullLogger<FileModelConfigStore>.Instance);
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
        services.AddSingleton<IModelConfigGate>(sp => new ModelConfigGate(sp.GetRequiredService<ISentinelAgentCatalog>(), sp.GetRequiredService<IAgentProfileBuilder>(), sp.GetRequiredService<IOptions<SentinelCoreSettings>>().Value));

        // Application shutdown token — injected into ViewModels so in-flight work
        // can be cancelled cooperatively when the app exits. CancellationToken is a
        // struct, so the non-generic registration overload is required.
        services.AddSingleton(typeof(CancellationToken), _ => ShutdownToken);

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

        // AddDbContextFactory registers BOTH IDbContextFactory<SentinelCoreDBContext> (singleton,
        // required by CaseFlowEngine) and the DbContext itself (scoped, required by
        // EvidenceStore/PatternMemoryStore/SignalRepository). AddDbContext alone would leave the
        // factory unresolvable.
        services.AddDbContextFactory<SentinelCoreDBContext>(options => { options.UseSqlServer(Environment.GetEnvironmentVariable("SENTINEL_CORE")); });
        services.AddDbContextFactory<SentinelRAGDBContext>(options => { options.UseSqlServer(Environment.GetEnvironmentVariable("REMOTEKB")); });



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








    /// <summary>
    ///     Writes startup exception details to the startup error log.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    private static void LogStartupException(Exception ex)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "SentinelCoreHost-startup-errors.log");
        string message = $"[{DateTime.Now:O}] {ex}\n";
        File.AppendAllText(logPath, message);
    }








    [LoggerMessage(LogLevel.Error, "Unhandled UI exception.")]
    static partial void LogUnhandledUiException(ILogger<App> logger, Exception exception);








    /// <summary>
    ///     Handles the current application domain's unhandled exception.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Provides data for the unhandled exception.</param>
    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogStartupException(ex);
        }
    }








    /// <summary>
    ///     Handles an unhandled exception raised on the UI dispatcher.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data for the unhandled dispatcher exception.</param>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ILogger<App>? logger = _host?.Services.GetService<ILogger<App>>();
        if (logger != null)
        {
            LogUnhandledUiException(logger, e.Exception);
        }

        e.Handled = false;
        LogStartupException(e.Exception);
        MessageBox.Show($"Unhandled dispatcher exception:\n\n{e.Exception}", "SentinelCore Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }








    private async void OnExit(object? sender, ExitEventArgs? e)
    {
        await InitiateShutdownAsync();
    }








    /// <summary>
    ///     Initializes application-wide exception handling and starts the application during startup.
    /// </summary>
    /// <param name="sender">The source of the startup event.</param>
    /// <param name="e">The startup event data.</param>
    private async void OnStartup(object? sender, StartupEventArgs e)
    {

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;


        try
        {
            await StartApplicationAsync(e);


            ILogger<App>? logger = Services.GetService<ILogger<App>>();
            logger?.LogInformation("SentinelCore Host application started successfully.");
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








    /// <summary>
    ///     Logs an unobserved task exception and marks it as observed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data that contains the unobserved exception.</param>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogStartupException(e.Exception);
        e.SetObserved();
    }








    /// <summary>
    ///     Initializes the application host and validates required startup configuration.
    /// </summary>
    /// <remarks>
    ///     The host is created with default configuration, the application base path is set, services
    ///     and logging are configured, and the host is started once.
    /// </remarks>
    /// <param name="e">The startup event arguments used to initialize the host.</param>
    /// <returns>A task that represents the asynchronous startup operation.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the required SENTINEL_CORE or REMOTEKB environment variables
    ///     are missing.
    /// </exception>
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
            throw new InvalidOperationException("Required environment variables are missing. Set SENTINEL_CORE (case flow database connection string) " + "and REMOTEKB (remote knowledge base connection string) before starting SentinelCore.");
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
                    logging.AddFileLogger();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

        await StartHostOnceAsync();
    }








    /// <summary>
    ///     Starts the <see cref="IHost" /> exactly once to ensure the application host is initialized and running.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <remarks>
    ///     This method ensures that the host is started only once, even if called multiple times.
    ///     If the host has already been started, the method returns immediately.
    ///     If the host fails to start, the <see cref="InvalidOperationException" /> is thrown.
    ///     Once the host is started, the application's main window is displayed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the <see cref="IHost" /> instance is null or if the host fails to start.
    /// </exception>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
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