// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         INavigationAware.cs
// Author: Kyle L. Crowler
// Build Num:  083003



namespace SentinelCore.UI.Services;


/// <summary>
///     Implemented by view-models that participate in Frame navigation.
///     <see cref="NavigationService" /> calls these hooks when the
///     current page changes so view-models can load or reset state.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    ///     Called when navigating away from the page associated with this view-model.
    /// </summary>
    void OnNavigatedFrom();

    /// <summary>
    ///     Called when navigating to the page associated with this view-model.
    /// </summary>
    /// <param name="parameter">Optional parameter passed from the previous page.</param>
    void OnNavigatedTo(object? parameter);
}
