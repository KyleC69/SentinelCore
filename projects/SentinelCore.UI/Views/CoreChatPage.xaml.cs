// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CoreChatPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the chat page.
///     Responsibilities scoped to this file:
///     • ViewModel wiring and DataContext assignment
///     • Auto-scroll to the latest message as items arrive
///     • Enter-to-send keyboard shortcut
/// </summary>
public partial class CoreChatPage : Page
{
    private readonly CoreChatViewModel _viewModel;








    /// <summary>
    ///     Creates the page, binds the view-model, and wires auto-scroll.
    /// </summary>
    /// <param name="viewModel">The chat view-model injected by DI.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel" /> is <c>null</c>.</exception>
    public CoreChatPage(CoreChatViewModel? viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
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








    /// <summary>
    ///     Scrolls the feed to the newest message whenever the collection changes.
    /// </summary>
    /// <summary>
    /// Handles changes to the Messages collection. When items are added,
    /// schedules a deferred scroll to the newest message so layout and
    /// the ItemContainerGenerator complete before calling ScrollIntoView.
    /// </summary>
    /// <param name="sender">The collection raising the event.</param>
    /// <param name="e">The collection change arguments.</param>
    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add)
        {
            // Post the scroll to the dispatcher at Background priority so the
            // collection-change processing and layout pass complete before we
            // call ScrollIntoView. This avoids ItemContainerGenerator
            // inconsistencies when virtualization/layout are still updating.
            MessagesListBox.Dispatcher.InvokeAsync(
                ScrollToBottom,
                System.Windows.Threading.DispatcherPriority.Background);
        }
    }








    private void OnUnloaded(object? sender, RoutedEventArgs? e)
    {
        Unloaded -= OnUnloaded;
        _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
    }








    /// <summary>
    ///     Posts the scroll after the current layout pass completes so the
    ///     ScrollViewer extent has already grown to accommodate new content.
    /// </summary>
    private void ScrollToBottom()
    {
        if (!MessagesListBox.Dispatcher.CheckAccess())
        {
            MessagesListBox.Dispatcher.InvokeAsync(ScrollToBottom);
            return;
        }

        if (MessagesListBox.Items.Count == 0)
        {
            return;
        }

        MessagesListBox.ScrollIntoView(MessagesListBox.Items[^1]);
    }
}