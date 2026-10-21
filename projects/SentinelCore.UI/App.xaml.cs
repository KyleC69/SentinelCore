// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         App.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.CaseFlowEngine.Infrastructure.DependencyInjection;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Data.DBEntities;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Infrastructure;
using SentinelCore.Orchestrations.Infrastructure.DependencyInjection;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;




namespace SentinelCore.UI;





/// <summary>
///     Composition root for the SentinelCore.UI application.
///     Wires the <see cref="IHost" /> container, registers all services,
///     and manages application lifecycle including graceful shutdown.
/// </summary>
public sealed partial class App
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
        SentinelCoreSettings sentinelSettings = new() { SqlConnectionString = Environment.GetEnvironmentVariable("SENTINEL_CORE") ?? string.Empty, TraceEnabled = true, TraceLogLevel = LogLevel.Trace, OrchestrationType = OrchestrationType.CustomGroup };

        // Seed Model profile cards from persisted configuration document so the
        // first orchestration after startup uses the user's saved configuration.
        FileModelConfigStore seedStore = new(NullLogger<FileModelConfigStore>.Instance);
        ModelConfigDocument? document = seedStore.Load();

        if (document is not null)
        {
            foreach (KeyValuePair<ModelTier, ModelProfile> entry in document.TierModels)
            {
                sentinelSettings.TierModels[entry.Key] = entry.Value;
            }
        }

        services.AddSentinelCore(sentinelSettings);



        // Case Flow Engine — optional module (PL-4/PL-5). The UI host opts in to the
        // real EF Core-backed engine, overriding the null-object defaults registered
        // by AddSentinelCore. Requires the DbContext factory registration below (PL-7).
        services.AddCaseFlowEngine();

        // UI layer — services, ViewModels, Views, and navigation
        services.AddSentinelCoreUi();


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
        //   services.AddDbContextFactory<SentinelRAGDBContext>(options => { options.UseSqlServer(Environment.GetEnvironmentVariable("REMOTEKB")); });



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
                Debug.WriteLine($"Error stopping host: {ex}");
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
            LogUnhandledUiException(logger, e.Exception!);
        }

        LogStartupException(e.Exception!);
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
            Debug.WriteLine($"[FATAL] Startup failed: {ex}");

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
        Debug.WriteLine("Starting host…");

        // Fail fast with a clear message when required configuration is missing;
        // otherwise EF Core surfaces a cryptic activation error much later.
        string? caseFlowConnection = Environment.GetEnvironmentVariable("SENTINEL_CORE");
        string? remoteKbConnection = Environment.GetEnvironmentVariable("REMOTEKB");

        if (string.IsNullOrWhiteSpace(caseFlowConnection) || string.IsNullOrWhiteSpace(remoteKbConnection))
        {
            throw new InvalidOperationException("Required environment variables are missing. Set SENTINEL_CORE (case flow database connection string) " + "and REMOTEKB (remote knowledge base connection string) before starting SentinelCore.");
        }





        string jPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SentinelCore", $"SentinelCore_json{DateTime.Now:yyyyMMdd}.log");

        string? appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c => { c.SetBasePath(appLocation ?? string.Empty); })
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    // WinExe has no attached console — Debug output surfaces in the VS Output window.
                    logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                    logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
                    logging.AddFilter("ModelContextProtocol.Client", LogLevel.Warning);
                    // Custom JSON logger provider is registered via DI in AddSentinelCore()
                    // and will be automatically resolved by the logging infrastructure.
                    // -- Custom Logging --
                    // Register CustomMAFFormatter and JsonLoggerProvider for structured logging
                    logging.AddFileLogger();



                    // Add the JsonLoggerProvider here explicitly
                    var formatter = new CustomMAFFormatter();
                    var loggerOptions = new JsonLoggerOptions
                    {
                        MinimumLevel = LogLevel.Trace,
                        Indented = true,
                        Output = JsonLoggerOutput.Console
                    };
                    logging.AddProvider(new JsonLoggerProvider(loggerOptions, formatter));


                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

        await StartHostOnceAsync(ShutdownToken);


        var aiStartlup = Services.GetRequiredService<AISystemsInitialization>();
        await aiStartlup.ExecuteAsync(ShutdownToken);
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

        // Initialize MCP servers after host is started
        await InitializeMcpServersAsync(cancellationToken).ConfigureAwait(false);

        // Show the main window after the host is running
        if (Current is null) return;

        Current.Dispatcher.Invoke(() =>
        {
            MainWindow = new MainWindow(_host.Services.GetRequiredService<INavigationService>());
            MainWindow.Show();
        });
    }








    /// <summary>
    ///     Loads persisted MCP server definitions and starts each server connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeMcpServersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var registry = _host!.Services.GetRequiredService<IMcpServerRegistry>();
            var logger = _host.Services.GetRequiredService<ILogger<App>>();

            if (registry is McpServerRegistry serverRegistry)
            {
                logger.LogInformation("Loading persisted MCP server definitions.");
                await serverRegistry.LoadPersistedAsync(cancellationToken).ConfigureAwait(false);

                // Start all loaded MCP servers.
                var serverInfos = await serverRegistry.ListAsync(cancellationToken);
                foreach (var info in serverInfos)
                {
                    try
                    {
                        logger.LogInformation("Starting MCP server {ServerId}.", info.Definition.Id);
                        await serverRegistry.StartAsync(info.Definition.Id, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to start MCP server {ServerId}.", info.Definition.Id);
                    }
                }
            }
            else
            {
                logger.LogWarning("MCP registry initializer expected {ExpectedType} but received {ActualType}.", typeof(McpServerRegistry).Name, registry.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            var logger = _host!.Services.GetRequiredService<ILogger<App>>();
            logger.LogError(ex, "Failed to initialize MCP servers.");
        }
    }
}
