// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         LogViewerWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using SentinelCore.UI.Services;




namespace SentinelCore.UI.Views;





/// <summary>
///     A simple WPF window that displays raw markdown log output from the console logger.
///     The Markdown property is bound to a collection that receives log entries unmutated.
/// </summary>
public sealed partial class LogViewerWindow : Window
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LogViewerWindow" /> class.
    /// </summary>
    public LogViewerWindow()
    {
        InitializeComponent();

        // Subscribe to raw log events from the logger provider
        LogViewerLoggerProvider.LogEntryReceived += OnLogEntryReceived;

        // Handle window closing to unsubscribe
        Closing += (_, _) => LogViewerLoggerProvider.LogEntryReceived -= OnLogEntryReceived;
    }








    /// <summary>
    ///     Collection of raw markdown log entries. Bound to MarkdownViewer.Markdown via StringJoin converter.
    /// </summary>
    public ObservableCollection<string> MarkdownLogEntries { get; } = [];








    /// <summary>
    ///     Handles a new log entry received from the logger provider.
    ///     Appends raw markdown directly to the bound collection.
    /// </summary>
    /// <param name="markdown">The raw markdown log entry.</param>
    private void OnLogEntryReceived(string markdown)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnLogEntryReceived(markdown));
            return;
        }

        MarkdownLogEntries.Add(markdown);
    }
}





/// <summary>
///     Converts an ObservableCollection<string> to a single markdown string by joining with newlines.
/// </summary>
public sealed class MarkdownCollectionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<string> entries)
        {
            return string.Join("\n\n", entries);
        }

        return string.Empty;
    }








    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}