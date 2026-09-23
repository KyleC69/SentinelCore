// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfClipboardService.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Windows;




namespace SentinelCore.UI.Services;





/// <summary>
///     WPF implementation of <see cref="IClipboardService" /> that delegates
///     to <see cref="Clipboard.SetText(string)" />.
/// </summary>
public sealed class WpfClipboardService : IClipboardService
{

    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Clipboard.SetText(text);
    }
}