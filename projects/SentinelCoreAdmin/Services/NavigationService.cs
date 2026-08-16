// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         NavigationService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;
using System.Windows.Navigation;

using JetBrains.Annotations;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;
using SentinelCoreAdmin.Helpers;




namespace SentinelCoreAdmin.Services;





public class NavigationService : INavigationService
{
    private Frame _frame;
    private object _lastParameterUsed;
    private readonly IPageService _pageService;








    public NavigationService([CanBeNull] IPageService pageService)
    {
        _pageService = pageService;
    }








    public bool CanGoBack
    {
        get => _frame.CanGoBack;
    }


    public void CleanNavigation() => _frame.CleanNavigation();








    public void GoBack()
    {
        if (_frame.CanGoBack)
        {
            object vmBeforeNavigation = _frame.GetDataContext();
            _frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }
        }
    }








    public void Initialize([CanBeNull] Frame shellFrame)
    {
        if (_frame == null)
        {
            _frame = shellFrame;
            _frame.Navigated += OnNavigated;
        }
    }








    public bool NavigateTo([CanBeNull] string pageKey, [CanBeNull] object parameter = null, bool clearNavigation = false)
    {
        Type pageType = _pageService.GetPageType(pageKey);

        if (_frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParameterUsed)))
        {
            _frame.Tag = clearNavigation;
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

            return navigated;
        }

        return false;
    }








    public event EventHandler<string> Navigated;








    public void UnsubscribeNavigation()
    {
        _frame.Navigated -= OnNavigated;
        _frame = null;
    }








    private void OnNavigated([CanBeNull] object sender, [CanBeNull] NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            bool clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.CleanNavigation();
            }

            object dataContext = frame.GetDataContext();
            if (dataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.ExtraData);
            }

            Navigated?.Invoke(sender, dataContext.GetType().FullName);
        }
    }
}