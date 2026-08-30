// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfClipboardService.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Windows;


namespace SentinelCore.UI.Services;


/// <summary>
///     WPF implementation of <see cref="IClipboardService" /> that delegates
///     to <see cref="Clipboard.SetText" />.
/// </summary>
public sealed class WpfClipboardService : IClipboardService
{
    /// <inheritdoc />
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Clipboard.SetText(text);
    }
}
