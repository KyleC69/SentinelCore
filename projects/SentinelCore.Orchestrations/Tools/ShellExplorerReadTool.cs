// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ShellExplorerReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying shell and Explorer settings.
/// </summary>
public sealed class ShellExplorerReadTool : AITool
{

    private const string AdvancedKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
    private const string ExplorerKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer";
    public override string Description { get; } = "Read-only tool for querying shell and Explorer settings.";
    public override string Name { get; } = "Shell_Explorer_Read";








    private static void ReadKeyValues(RegistryKey root, string keyPath, StringBuilder sb, string[] valueNames)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"[{keyPath}]");
        foreach (string valueName in valueNames)
        {
            object? value = key.GetValue(valueName);
            if (value is not null)
            {
                sb.AppendLine($"  {valueName}={value}");
            }
        }
    }








    [Description("Reads common Explorer settings such as hidden files and file extensions.")]
    public Task<ToolResult> shell_explorer_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(Registry.CurrentUser, AdvancedKey, sb, ["Hidden", "ShowSuperHidden", "HideFileExt", "LaunchTo"]);
            ReadKeyValues(Registry.CurrentUser, ExplorerKey, sb, ["EnableAutoTray", "ShellState"]);

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Shell Explorer settings read failed: {ex.Message}"));
        }
    }








    [Description("Lists pinned items in the Windows taskbar Quick Launch/User Pinned path.")]
    public Task<ToolResult> shell_taskbar_pinned_list()
    {
        try
        {
            string pinnedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");
            if (!Directory.Exists(pinnedPath))
            {
                return Task.FromResult(ToolResult.Fail($"Taskbar pinned path not found: {pinnedPath}"));
            }

            StringBuilder sb = new();
            foreach (string file in Directory.GetFiles(pinnedPath, "*.lnk")) sb.AppendLine(Path.GetFileName(file));

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Taskbar pinned listing failed: {ex.Message}"));
        }
    }
}