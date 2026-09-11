// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfDialogService.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.Windows;

using SentinelCore.UI.Views;




namespace SentinelCore.UI.Services;





/// <summary>
///     WPF implementation of <see cref="IDialogService" /> that shows a themed
///     modal <see cref="ConfirmationDialog" /> owned by the application's main window.
/// </summary>
public sealed class WpfDialogService : IDialogService
{
    /// <inheritdoc />
    public bool Confirm(string title, string message, string confirmButtonText, bool isDestructive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmButtonText);

        Window? owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        ConfirmationDialog dialog = new(title, message, confirmButtonText, isDestructive) { Owner = owner };

        return dialog.ShowDialog() == true;
    }
}