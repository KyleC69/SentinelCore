// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IFolderBrowserService.cs
// Author: Kyle L. Crowder
// Build Num:  091300



namespace SentinelCore.UI.Services;





/// <summary>
///     Abstraction over the OS folder-picker dialog so ViewModels can request
///     a directory path without referencing WPF dialog types, keeping them unit-testable.
/// </summary>
public interface IFolderBrowserService
{
    /// <summary>
    ///     Shows a modal folder-picker dialog.
    /// </summary>
    /// <param name="title">The dialog title shown to the user.</param>
    /// <param name="initialDirectory">The directory the dialog opens in; ignored when empty or missing.</param>
    /// <returns>The selected folder path, or <c>null</c> when the user cancelled.</returns>
    string? BrowseFolder(string title, string initialDirectory);
}