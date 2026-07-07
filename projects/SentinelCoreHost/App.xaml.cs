// Solution: SentinelCoreLib
// Project:   SentinelCoreHost
// File:         App.xaml.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.IO;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Contracts;

using SentinelCoreHost.ViewModels;

using SentinelCoreLib.Application;
using SentinelCoreLib.Hosting;




namespace SentinelCoreHost;





public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IServiceCollection _services = new ServiceCollection();
    private SentinelCoreSettings _settings;








    public App()
    {


        _settings = new SentinelCoreSettings
        {
                SqlConnectionString = "server=DESKTOP-NC01091; Database=SentinelCore;Integrated Security=true; TrustServerCertificate=true",
                AgentTraceEnabled = true,
                AgentTraceLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace,
                CoreModel = new ModelSettings { Endpoint = "http://127.0.0.1:11434", ModelId = "gpt-oss:120b-cloud", Temperature = 0.1f },
                DomainModel = new ModelSettings { Endpoint = "http://127.0.0.1:11434", ModelId = "gpt-oss:20b-cloud", Temperature = 0.3f },
                ManagerModel = new ModelSettings { Endpoint = "http://127.0.0.1:11434", ModelId = "gpt-oss:20b-cloud", Temperature = 0.3f }
        };


    }








    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            IServiceCollection svcs = RegisterServices(_services);
            _serviceProvider = svcs.BuildServiceProvider();

            MainWindowViewModel viewModel = new(_serviceProvider.GetRequiredService<InvestigationControl>());
            MainWindow window = new(viewModel);
            window.Show();
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








    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogStartupException(ex);
        }
    }








    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
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
        services.AddSingleton<MainWindowViewModel>();
        services.AddSentinelCore(_settings!);

        return services;
    }
}