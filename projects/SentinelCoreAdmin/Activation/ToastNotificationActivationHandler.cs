// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ToastNotificationActivationHandler.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;

using SentinelCoreAdmin.Contracts.Activation;
using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.Views;




namespace SentinelCoreAdmin.Activation;





// For more information about sending a local toast notification from C# apps, see
// https://docs.microsoft.com/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
// and https://github.com/microsoft/TemplateStudio/blob/main/docs/WPF/features/toast-notifications.md
public class ToastNotificationActivationHandler : IActivationHandler
{

    private readonly IConfiguration _config;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    public const string ActivationArguments = "ToastNotificationActivationArguments";








    public ToastNotificationActivationHandler([CanBeNull] IConfiguration config, [CanBeNull] IServiceProvider serviceProvider, [CanBeNull] INavigationService navigationService)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
    }








    public bool CanHandle()
    {
        return !string.IsNullOrEmpty(_config[ActivationArguments]);
    }








    public async Task HandleAsync()
    {
        if (App.Current.Windows.OfType<IShellWindow>().Count() == 0)
        {
            // Here you can get an instance of the ShellWindow and choose navigate
            // to a specific page depending on the toast notification arguments
        }
        else
        {
            App.Current.MainWindow.Activate();
            if (App.Current.MainWindow.WindowState == WindowState.Minimized)
            {
                App.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        await Task.CompletedTask;
    }
}