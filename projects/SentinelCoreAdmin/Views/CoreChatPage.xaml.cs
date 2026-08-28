// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CoreChatPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.AI;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





/// <summary>
///     Code-behind for the chat page.
///     Responsibilities scoped to this file:
///     • ViewModel wiring and DataContext assignment
///     • Auto-scroll to latest message (new message + streaming content updates)
///     • Enter-to-send keyboard shortcut
/// </summary>
public partial class CoreChatPage : Page
{
    private readonly CoreChatViewModel _viewModel;








    public CoreChatPage([CanBeNull] CoreChatViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.Messages.CollectionChanged += (_, _) => ScrollToBottom();
        Unloaded += OnUnloaded;
    }








    /// <summary>
    ///     Enter → send.  Shift+Enter → natural newline (handled by TextBox).
    ///     Setting e.Handled = true prevents the TextBox from inserting a newline.
    /// </summary>
    private void InputBox_KeyDown([CanBeNull] object sender, [NotNull] KeyEventArgs e)
    {
        if (e.Key is Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (_viewModel.SendCommand.CanExecute(null))
            {
                _viewModel.SendCommand.Execute(null);
            }
        }
    }








    private void Message_PropertyChanged(object? sender, [NotNull] PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatMessage.Text))
        {
            ScrollToBottom();
        }
    }








    private void OnUnloaded([CanBeNull] object sender, [CanBeNull] RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        _viewModel.Dispose();
    }








    /// <summary>
    ///     Posts the scroll after the current layout pass completes so the
    ///     ScrollViewer extent has already grown to accommodate new content.
    /// </summary>
    private void ScrollToBottom()
    {
        /*
        if (!MessagesListBox.Dispatcher.CheckAccess())
        {
            MessagesListBox.Dispatcher.Invoke(ScrollToBottom);
            return;
        }

        if (MessagesListBox.Items.Count == 0)
        {
            return;
        }

        MessagesListBox.ScrollIntoView(MessagesListBox.Items[^1]);
        */
    }
}