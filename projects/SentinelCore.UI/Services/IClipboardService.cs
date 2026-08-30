// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IClipboardService.cs
// Author: Kyle L. Crowler
// Build Num:  083003



namespace SentinelCore.UI.Services;


/// <summary>
///     Abstraction over the system clipboard to decouple ViewModels
///     from <see cref="System.Windows.Clipboard" />.
///     This enables unit testing of ViewModels without a running WPF application.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    ///     Copies the specified text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    void SetText(string text);
}
