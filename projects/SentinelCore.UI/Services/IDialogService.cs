// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IDialogService.cs
// Author: Kyle L. Crowler
// Build Num:  091003



namespace SentinelCore.UI.Services;





/// <summary>
///     Abstraction over modal dialogs so ViewModels can request user
///     confirmations without referencing WPF window types, keeping them unit-testable.
/// </summary>
public interface IDialogService
{
    /// <summary>
    ///     Shows a modal confirmation dialog with Confirm and Cancel buttons.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="message">The body text explaining what is about to happen.</param>
    /// <param name="confirmButtonText">The label of the confirmation button.</param>
    /// <param name="isDestructive">
    ///     When <c>true</c>, the confirm button is styled with the danger palette
    ///     so destructive actions are visually distinct.
    /// </param>
    /// <returns><c>true</c> when the user confirmed; <c>false</c> when cancelled.</returns>
    bool Confirm(string title, string message, string confirmButtonText, bool isDestructive);
}
