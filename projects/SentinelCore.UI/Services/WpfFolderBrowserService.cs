// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfFolderBrowserService.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.IO;
using System.Windows;

using Microsoft.Win32;




namespace SentinelCore.UI.Services;





/// <summary>
///     WPF implementation of <see cref="IFolderBrowserService" /> backed by
///     <see cref="OpenFolderDialog" /> (.NET 8+ WPF).
/// </summary>
public sealed class WpfFolderBrowserService : IFolderBrowserService
{
    /// <inheritdoc />
    public string? BrowseFolder(string title, string initialDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        OpenFolderDialog dialog = new() { Title = title };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        Window? owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }
}