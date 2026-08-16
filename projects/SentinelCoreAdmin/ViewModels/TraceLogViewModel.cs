// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         TraceLogViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using MahApps.Metro.Controls;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;




namespace SentinelCoreAdmin.ViewModels;





public partial class TraceLogViewModel : ObservableObject, INavigationAware
{

    [ObservableProperty] private Visibility _failedMesageVisibility = Visibility.Collapsed;

    [ObservableProperty] private bool _isLoading = true;

    [ObservableProperty] private Visibility _isLoadingVisibility = Visibility.Visible;

    [ObservableProperty] private bool _isShowingFailedMessage;

    private readonly IRightPaneService _rightPaneService;

    [ObservableProperty] private string _source;

    private readonly ISystemService _systemService;

    private WebView2 _webView;

    // TODO: Set the URI of the page to show by default
    private const string DefaultUrl = "https://docs.microsoft.com/windows/apps/";








    public TraceLogViewModel([CanBeNull] ISystemService systemService, [CanBeNull] IRightPaneService rightPaneService)
    {
        _systemService = systemService;
        Source = DefaultUrl;
        _rightPaneService = rightPaneService;
    }








    public void OnNavigatedFrom()
    {
        _rightPaneService.PaneOpened -= OnRightPaneOpened;
        _rightPaneService.PaneClosed -= OnRightPaneClosed;
    }








    public void OnNavigatedTo([CanBeNull] object parameter)
    {
        _rightPaneService.PaneOpened += OnRightPaneOpened;
        _rightPaneService.PaneClosed += OnRightPaneClosed;
    }








    [RelayCommand(CanExecute = nameof(CanBrowserBack))]
    private void BrowserBack() => _webView?.GoBack();








    [RelayCommand(CanExecute = nameof(CanBrowserForward))]
    private void BrowserForward() => _webView?.GoForward();








    private bool CanBrowserBack() => _webView?.CanGoBack ?? false;


    private bool CanBrowserForward() => _webView?.CanGoForward ?? false;


    public void Initialize([CanBeNull] WebView2 webView) => _webView = webView;








    partial void OnIsLoadingChanged(bool value)
    {
        IsLoadingVisibility = value ? Visibility.Visible : Visibility.Collapsed;
    }








    partial void OnIsShowingFailedMessageChanged(bool value)
    {
        FailedMesageVisibility = value ? Visibility.Visible : Visibility.Collapsed;
    }








    public void OnNavigationCompleted([CanBeNull] object sender, [CanBeNull] CoreWebView2NavigationCompletedEventArgs e)
    {
        IsLoading = false;
        if (e != null && !e.IsSuccess)
        {
            // Use `e.WebErrorStatus` to vary the displayed message based on the error reason
            IsShowingFailedMessage = true;
        }

        BrowserBackCommand.NotifyCanExecuteChanged();
        BrowserForwardCommand.NotifyCanExecuteChanged();
    }








    private void OnRightPaneClosed([CanBeNull] object sender, [CanBeNull] EventArgs e) => _webView.Margin = new Thickness(0);








    private void OnRightPaneOpened([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
        // WebView control is always rendered on top
        // We need to adapt the WebView to be able to show the right pane
        if (sender is SplitView splitView)
        {
            _webView.Margin = new Thickness(0, 0, splitView.OpenPaneLength, 0);
        }
    }








    [RelayCommand]
    private void OpenInBrowser() => _systemService.OpenInWebBrowser(Source);








    [RelayCommand]
    private void Refresh()
    {
        IsShowingFailedMessage = false;
        IsLoading = true;
        _webView?.Reload();
    }
}