// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         MainWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Microsoft.Extensions.AI;

using SentinelCoreHost.ViewModels;




namespace SentinelCoreHost;





/// <summary>
///     Interaction logic for MainWindow.xaml.
///     Responsibilities scoped to this code-behind:
///     • ViewModel wiring and DataContext assignment
///     • Auto-scroll to latest message (new message + streaming content updates)
///     • Enter-to-send keyboard shortcut
///     • Win32 DWM dark title-bar opt-in
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;








    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.Messages.CollectionChanged += (_, _) => ScrollToBottom();

        // Apply dark chrome after the Win32 window handle is available.
        SourceInitialized += (_, _) => EnableDarkTitleBar(this);

        InputBox.Text = "CASEGEN: Search Event logs for warnings and errors and create a case for each";

    }








    // ── Dark title-bar (Windows 10 20H1+ / Windows 11) ──────────────────────








    // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);








    private static void EnableDarkTitleBar(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
    }








    // ── Keyboard handling ────────────────────────────────────────────────────








    /// <summary>
    ///     Enter → send.  Shift+Enter → natural newline (handled by TextBox).
    ///     Setting e.Handled = true prevents the TextBox from inserting a newline.
    /// </summary>
    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (_viewModel.SendCommand.CanExecute(null)) _viewModel.SendCommand.Execute(null);
        }
    }








    private void Message_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatMessage.Text)) ScrollToBottom();
    }








    // ── Auto-scroll ──────────────────────────────────────────────────────────








    /// <summary>
    ///     Posts the scroll after the current layout pass completes so the
    ///     ScrollViewer extent has already grown to accommodate new content.
    /// </summary>
    private void ScrollToBottom()
    {
        if (!MessagesListBox.Dispatcher.CheckAccess())
        {
            MessagesListBox.Dispatcher.Invoke(ScrollToBottom);
            return;
        }

        if (MessagesListBox.Items.Count == 0)
            return;

        MessagesListBox.ScrollIntoView(MessagesListBox.Items[^1]);

    }
}