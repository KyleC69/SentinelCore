// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         App.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.IO;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.CaseEngine;
using SentinelCore.Contracts;
using SentinelCore.Events;
using SentinelCore.Infrastructure.DependencyInjection;

using SentinelCoreHost.ViewModels;




namespace SentinelCoreHost;





/// <summary>
///     This class is intended to be minimal and should only contain the necessary code to bootstrap the application and
///     handle startup and shutdown events.
///     The library is repsonsible for the bulk of the application logic and orchestration, while this class serves as the
///     entry point for the WPF application.
/// </summary>
public sealed partial class App : Application
{

    private IHost? _host;
    private bool _isHostStarted;
    private IServiceProvider? _serviceProvider;
    private readonly SentinelCoreSettings _settings;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreHost.App" /> class.
    /// </summary>
    /// <remarks>
    ///     This constructor sets up the application with default settings for
    ///     <see cref="SentinelCore.Contracts.SentinelCoreSettings" /> and initializes
    ///     the required configurations for the application runtime.
    /// </remarks>
    public App()
    {

        // Initialize default settings for SentinelCore
        //Single source of settings for the application.
        // Setting obj is passed into AddSentinelCore() to configure the core library.
        _settings = new SentinelCoreSettings
        {
                SqlConnectionString = "server=DESKTOP-NC01091;Database=SentinelCore;Integrated Security=true; TrustServerCertificate=true",
                TraceEnabled = true,
                TraceLogLevel = LogLevel.Trace,
                OrchestrationType = OrchestrationType.CustomGroup,
                DefaultModel = new ModelProfile("http://127.0.0.1:11434", "glm-5.1:cloud", .2f, 15000, 1, .2f),
                DefaultUtilityModel = new ModelProfile("http://127.0.0.1:11434", "glm-5.1:cloud", 0.1f, 12000, 1, 0.3f)
        };


        // Use explicit shutdown so the IHost lifetime controls app exit,
        // preventing WPF from shutting down before background services stop.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }








    public static App CurrentApp
    {
        get => (App)Current;
    }

    public IServiceProvider Services
    {
        get => _host?.Services ?? throw new InvalidOperationException("The application host is not available.");
    }

    // Initialise to avoid CS8618 warning.
    public TraceLogWindow traceWindow { get; set; } = null!;








    protected override void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            try
            {
                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }
            catch (Exception ex)
            {
                LogStartupException(ex);
            }
        }

        base.OnExit(e);
    }








    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            _host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(config => { config.SetBasePath(Environment.CurrentDirectory); })
                    .ConfigureServices((c, s) =>
                    {
                        c.HostingEnvironment.ApplicationName = "SentinelCore";
                        c.HostingEnvironment.EnvironmentName = "Development";
                        RegisterServices(s);
                    })
                    .Build();

            _serviceProvider = _host.Services;


            //TODO: Refactor back to minimalist and abstracted.
            MainWindowViewModel viewModel = new(_serviceProvider.GetRequiredService<IOrchestrationControl>(), _serviceProvider.GetRequiredService<ISentinelCoreEvents>(), _serviceProvider.GetRequiredService<ICaseFlowEngine>(), _serviceProvider.GetRequiredService<ILogger<MainWindowViewModel>>());


            MainWindow window = new(viewModel);
            MainWindow = window;

            // Show the window immediately so the user sees the UI.
            // The host (including database migrations) is started asynchronously
            // so it does not block the UI thread during startup.
            window.Show();

            // Start the host on a background thread so DB migrations
            // and other IHostedService startups don't freeze the UI.
            _ = StartHostAsync();

            ILogger<App>? logger = Services.GetService<ILogger<App>>();
            logger?.LogInformation("SentinelCore Host application started successfully.");


        }
        catch (Exception ex)
        {
            LogStartupException(ex);
            MessageBox.Show($"Startup failed:\n\n{ex}\n\nSee the application log for details.", "SentinelCore Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown(1);
        }
    }








    private static void LogStartupException(Exception ex)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "SentinelCoreHost-startup-errors.log");
        string message = $"[{DateTime.Now:O}] {ex}\n";
        File.AppendAllText(logPath, message);
    }








    [LoggerMessage(LogLevel.Error, "Unhandled UI exception.")]
    static partial void LogUnhandledUiException(ILogger<App> logger, Exception exception);








    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogStartupException(ex);
        }
    }








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








    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogStartupException(e.Exception);
        e.SetObserved();
    }








    private IServiceCollection RegisterServices(IServiceCollection services)
    {

        services.AddLogging(op =>
        {
            op.AddConsole();
            //op.AddJsonConsole();
            op.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddSentinelCore(_settings);

        return services;
    }








    private async Task StartHostAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                _host!.Start();
                _isHostStarted = true;
            });
        }
        catch (Exception ex)
        {
            LogStartupException(ex);
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Host startup failed:\n\n{ex}\n\nSee the application log for details.", "SentinelCore Host Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown(1);
            });
        }
    }
}