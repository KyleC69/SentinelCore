// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         WindowManagerService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using JetBrains.Annotations;

using MahApps.Metro.Controls;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;
using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.Helpers;




namespace SentinelCoreAdmin.Services;





public class WindowManagerService : IWindowManagerService
{
    private readonly IPageService _pageService;
    private readonly IServiceProvider _serviceProvider;








    public WindowManagerService([CanBeNull] IServiceProvider serviceProvider, [CanBeNull] IPageService pageService)
    {
        _serviceProvider = serviceProvider;
        _pageService = pageService;
    }








    [CanBeNull]
    public Window GetWindow([CanBeNull] string key)
    {
        foreach (Window window in Application.Current.Windows)
        {
            object dataContext = window.GetDataContext();
            if (dataContext?.GetType().FullName == key)
            {
                return window;
            }
        }

        return null;
    }








    [CanBeNull]
    public Window MainWindow
    {
        get => Application.Current.MainWindow;
    }








    public bool? OpenInDialog([CanBeNull] string key, [CanBeNull] object parameter = null)
    {
        Window shellWindow = _serviceProvider.GetService(typeof(IShellDialogWindow)) as Window;
        Frame frame = ((IShellDialogWindow)shellWindow).GetDialogFrame();
        frame.Navigated += OnNavigated;
        shellWindow.Closed += OnWindowClosed;
        Page page = _pageService.GetPage(key);
        bool navigated = frame.Navigate(page, parameter);
        return shellWindow.ShowDialog();
    }








    public void OpenInNewWindow([CanBeNull] string key, [CanBeNull] object parameter = null)
    {
        Window window = GetWindow(key);
        if (window != null)
        {
            window.Activate();
        }
        else
        {
            window = new MetroWindow { Title = "SentinelCoreAdmin", Style = Application.Current.FindResource("CustomMetroWindow") as Style };
            Frame frame = new() { Focusable = false, NavigationUIVisibility = NavigationUIVisibility.Hidden };

            window.Content = frame;
            Page page = _pageService.GetPage(key);
            window.Closed += OnWindowClosed;
            window.Show();
            frame.Navigated += OnNavigated;
            bool navigated = frame.Navigate(page, parameter);
        }
    }








    private void OnNavigated([CanBeNull] object sender, [CanBeNull] NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            object dataContext = frame.GetDataContext();
            if (dataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.ExtraData);
            }
        }
    }








    private void OnWindowClosed([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
        if (sender is Window window)
        {
            if (window.Content is Frame frame)
            {
                frame.Navigated -= OnNavigated;
            }

            window.Closed -= OnWindowClosed;
        }
    }
}