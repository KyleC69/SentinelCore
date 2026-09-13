// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         NavigationService.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.Windows.Controls;

using Microsoft.Extensions.Logging;




namespace SentinelCore.UI.Services;





/// <summary>
///     Frame-based navigation backed by <see cref="IViewLocator" />.
///     Resolves pages via the locator (which uses DI), raises
///     <see cref="INavigationAware" /> hooks on the outgoing and
///     incoming view-models, and disposes view-models that implement
///     <see cref="IDisposable" /> when their page is navigated away from.
/// </summary>
public sealed class NavigationService : INavigationService
{

    private readonly ILogger<NavigationService> _logger;
    private Frame? _shellFrame;

    private readonly IViewLocator _viewLocator;








    /// <summary>
    ///     Creates a new <see cref="NavigationService" /> with the given view locator.
    /// </summary>
    /// <param name="viewLocator">The view locator that resolves pages from view-model type keys.</param>
    /// <param name="logger">The logger for navigation failures and lifecycle events.</param>
    public NavigationService(IViewLocator viewLocator, ILogger<NavigationService> logger)
    {
        _viewLocator = viewLocator ?? throw new ArgumentNullException(nameof(viewLocator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }








    /// <inheritdoc />
    public void Initialize(Frame shellFrame)
    {
        _shellFrame = shellFrame;
    }








    /// <inheritdoc />
    public bool NavigateTo(string? pageKey, object? parameter = null)
    {
        if (_shellFrame is null || string.IsNullOrWhiteSpace(pageKey))
        {
            _logger.LogWarning("Navigation requested before the shell frame was initialized (page key: '{PageKey}').", pageKey);
            return false;
        }

        Page? newPage = _viewLocator.ResolvePage(pageKey);

        if (newPage is null)
        {
            _logger.LogError("No page is registered for view-model key '{PageKey}'.", pageKey);
            return false;
        }

        if (_shellFrame.Content is Page oldPage)
        {
            DeactivatePage(oldPage);
        }

        if (newPage.DataContext is INavigationAware newAware)
        {
            newAware.OnNavigatedTo(parameter);
        }

        _shellFrame.Navigate(newPage);

        // The Frame journal keeps every navigated page alive for the app lifetime;
        // this shell uses tab-style navigation, so the journal is cleared after
        // every hop to release the transient pages and their view-models.
        ClearJournal();

        Navigated?.Invoke(this, pageKey);

        return true;
    }








    public event EventHandler<string>? Navigated;








    /// <summary>
    ///     Removes every entry from the frame's navigation journal so transient
    ///     pages are not retained by the back stack.
    /// </summary>
    private void ClearJournal()
    {
        if (_shellFrame?.NavigationService is null)
        {
            return;
        }

        while (_shellFrame.NavigationService.RemoveBackEntry() is not null)
        {
            // Drain the journal entry by entry.
        }
    }








    /// <summary>
    ///     Tears down the outgoing page: raises <see cref="INavigationAware.OnNavigatedFrom" />
    ///     and disposes the page's view-model when it implements <see cref="IDisposable" />.
    /// </summary>
    /// <param name="page">The page being navigated away from.</param>
    private void DeactivatePage(Page page)
    {
        if (page.DataContext is INavigationAware previousAware)
        {
            previousAware.OnNavigatedFrom();
        }

        if (page.DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}