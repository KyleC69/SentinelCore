// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         TraceLogWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.RegularExpressions;
using System.Windows;




namespace SentinelCoreHost;





/// <summary>
///     Minimal non-modal window that receives raw trace lines from
///     appends them to a scrolling     text area.  No decoration — just the output.
/// </summary>
public partial class TraceLogWindow : Window
{
    private static readonly Regex AnsiEscapeRegex = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);








    /// <summary>
    ///     Initializes a new instance of the <see cref="TraceLogWindow" /> class.
    /// </summary>
    public TraceLogWindow()
    {
        InitializeComponent();
    }








    public void AppendLog(string jsonLog)
    {
        if (Dispatcher.CheckAccess())
            AppendLogInternal(jsonLog);
        else
            Dispatcher.BeginInvoke(() => AppendLogInternal(jsonLog));
    }








    private void AppendLogInternal(string jsonLog)
    {
        LogRichTextBox.AppendText(jsonLog);
        LogRichTextBox.ScrollToEnd();
    }
}