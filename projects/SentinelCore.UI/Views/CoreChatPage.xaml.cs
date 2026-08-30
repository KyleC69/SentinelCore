// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CoreChatPage.xaml.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using Microsoft.Extensions.AI;

using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;


/// <summary>
///     Code-behind for the chat page.
///     Responsibilities scoped to this file:
///     • ViewModel wiring and DataContext assignment
///     • Collection synchronization for cross-thread message updates
///     • Auto-scroll to latest message (new message + streaming content updates)
///     • Enter-to-send keyboard shortcut
/// </summary>
public partial class CoreChatPage : Page
{
    private readonly CoreChatViewModel _viewModel;





    public CoreChatPage(CoreChatViewModel? viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;

        // Enable cross-thread collection synchronization using the ViewModel's sync root.
        // This keeps the ViewModel free of WPF-specific BindingOperations calls.
        BindingOperations.EnableCollectionSynchronization(_viewModel.Messages, _viewModel.MessagesSyncRoot);

        _viewModel.Messages.CollectionChanged += (_, _) => ScrollToBottom();
        Unloaded += OnUnloaded;
    }





    /// <summary>
    ///     Enter → send.  Shift+Enter → natural newline (handled by TextBox).
    ///     Setting e.Handled = true prevents the TextBox from inserting a newline.
    /// </summary>
    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
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





    private void Message_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatMessage.Text))
        {
            ScrollToBottom();
        }
    }





    private void OnUnloaded(object? sender, RoutedEventArgs? e)
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