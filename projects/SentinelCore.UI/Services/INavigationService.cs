// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         INavigationService.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Windows.Controls;



namespace SentinelCore.UI.Services;





/// <summary>
///     Minimal Frame-based navigation contract for the SentinelCore UI.
///     Pages are resolved from the DI container by view-model type so
///     view-models never reference concrete views.
/// </summary>
public interface INavigationService
{

    /// <summary>
    ///     Raised after a page change to allow shell elements (e.g. the
    ///     nav bar) to update their highlighted state.
    /// </summary>
    event EventHandler<string>? Navigated;


    /// <summary>
    ///     Binds the service to the shell's <see cref="Frame" />. Must be
    ///     called once before the first <see cref="NavigateTo" />.
    /// </summary>
    /// <param name="shellFrame">The main content frame of the shell window.</param>
    void Initialize(Frame shellFrame);


    /// <summary>
    ///     Navigates the shell frame to the page associated with a view-model type.
    /// </summary>
    /// <param name="pageKey">The view-model type full name identifying the page.</param>
    /// <param name="parameter">Optional parameter passed to <c>OnNavigatedTo</c>.</param>
    /// <param name="clearNavigation">Removes back-stack entries after navigating.</param>
    /// <returns>True when navigation occurred.</returns>
    bool NavigateTo(string? pageKey, object? parameter = null, bool clearNavigation = false);
}