// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         NavigationService.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Windows.Controls;




namespace SentinelCore.UI.Services;


/// <summary>
///     Frame-based navigation backed by <see cref="IViewLocator" />.
///     Resolves pages via the locator (which uses DI), and raises
///     <see cref="INavigationAware" /> hooks on the outgoing and
///     incoming view-models.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private Frame? _shellFrame;

    private readonly IViewLocator _viewLocator;





    public event EventHandler<string>? Navigated;





    /// <summary>
    ///     Creates a new <see cref="NavigationService" /> with the given view locator.
    /// </summary>
    /// <param name="viewLocator">The view locator that resolves pages from view-model type keys.</param>
    public NavigationService(IViewLocator viewLocator)
    {
        _viewLocator = viewLocator ?? throw new ArgumentNullException(nameof(viewLocator));
    }





    /// <inheritdoc />
    public void Initialize(Frame shellFrame)
    {
        _shellFrame = shellFrame;
    }





    /// <inheritdoc />
    public bool NavigateTo(string? pageKey, object? parameter = null, bool clearNavigation = false)
    {
        if (_shellFrame is null || string.IsNullOrWhiteSpace(pageKey))
        {
            return false;
        }

        Page? newPage = _viewLocator.ResolvePage(pageKey);

        if (newPage is null)
        {
            return false;
        }

        if (_shellFrame.Content is Page oldPage && oldPage.DataContext is INavigationAware previousAware)
        {
            previousAware.OnNavigatedFrom();
        }

        if (newPage.DataContext is INavigationAware newAware)
        {
            newAware.OnNavigatedTo(parameter);
        }

        _shellFrame.Navigate(newPage);

        if (clearNavigation)
        {
            _shellFrame.NavigationService.RemoveBackEntry();
        }

        Navigated?.Invoke(this, pageKey);

        return true;
    }
}