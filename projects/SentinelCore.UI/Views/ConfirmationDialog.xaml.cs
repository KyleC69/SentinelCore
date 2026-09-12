// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ConfirmationDialog.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.Windows;

using SentinelCore.UI.Services;




namespace SentinelCore.UI.Views;





/// <summary>
///     Themed modal confirmation dialog used by <see cref="WpfDialogService" />.
///     Destructive confirmations restyle the confirm button with the danger palette.
/// </summary>
public partial class ConfirmationDialog : Window
{
    /// <summary>
    ///     Creates the dialog with the given title, message, and confirm button label.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The body message shown to the user.</param>
    /// <param name="confirmButtonText">The confirm button label.</param>
    /// <param name="isDestructive">When <c>true</c>, the confirm button uses the danger palette.</param>
    public ConfirmationDialog(string title, string message, string confirmButtonText, bool isDestructive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmButtonText);

        InitializeComponent();

        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmButtonText;

        if (isDestructive)
        {
            ConfirmButton.SetResourceReference(BackgroundProperty, "ChatDanger");
            ConfirmButton.SetResourceReference(ForegroundProperty, "ChatTextPrimary");
        }
    }








    /// <summary>
    ///     Closes the dialog with a cancelled result.
    /// </summary>
    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }








    /// <summary>
    ///     Closes the dialog with a confirmed result.
    /// </summary>
    private void OnConfirmClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}