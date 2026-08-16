// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         RightPaneService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;
using System.Windows.Navigation;

using JetBrains.Annotations;

using MahApps.Metro.Controls;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;
using SentinelCoreAdmin.Helpers;




namespace SentinelCoreAdmin.Services;





public class RightPaneService : IRightPaneService
{
    private Frame _frame;
    private object _lastParameterUsed;
    private readonly IPageService _pageService;
    private SplitView _splitView;








    public RightPaneService([CanBeNull] IPageService pageService)
    {
        _pageService = pageService;
    }








    public void CleanUp()
    {
        _frame.Navigated -= OnNavigated;
        _splitView.PaneClosed -= OnPaneClosed;
    }








    public void Initialize([CanBeNull] Frame rightPaneFrame, [CanBeNull] SplitView splitView)
    {
        _frame = rightPaneFrame;
        _splitView = splitView;
        _frame.Navigated += OnNavigated;
        _splitView.PaneClosed += OnPaneClosed;
    }








    public void OpenInRightPane([CanBeNull] string pageKey, [CanBeNull] object parameter = null)
    {
        Type pageType = _pageService.GetPageType(pageKey);
        if (_frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParameterUsed)))
        {
            Page page = _pageService.GetPage(pageKey);
            bool navigated = _frame.Navigate(page, parameter);
            if (navigated)
            {
                _lastParameterUsed = parameter;
                object dataContext = _frame.GetDataContext();
                if (dataContext is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }
        }

        _splitView.IsPaneOpen = true;
        PaneOpened?.Invoke(_splitView, EventArgs.Empty);
    }








    public event EventHandler PaneClosed;

    public event EventHandler PaneOpened;








    private void OnNavigated([CanBeNull] object sender, [CanBeNull] NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            frame.CleanNavigation();
            object dataContext = frame.GetDataContext();
            if (dataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.ExtraData);
            }
        }
    }








    private void OnPaneClosed([CanBeNull] object sender, [CanBeNull] EventArgs e) => PaneClosed?.Invoke(sender, e);
}