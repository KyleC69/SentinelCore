// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IViewLocator.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Windows.Controls;


namespace SentinelCore.UI.Services;


/// <summary>
///     Resolves a <see cref="Page" /> for a given view-model type key.
///     This decouples <see cref="NavigationService" /> from hard-coded
///     view-model→page mappings, allowing new pages to be added via
///     DI registration without modifying the navigation service.
/// </summary>
public interface IViewLocator
{
    /// <summary>
    ///     Attempts to resolve a <see cref="Page" /> for the given
    ///     view-model type full name.
    /// </summary>
    /// <param name="pageKey">The view-model type full name identifying the page.</param>
    /// <returns>The resolved page, or <c>null</c> if no mapping exists.</returns>
    Page? ResolvePage(string pageKey);
}
