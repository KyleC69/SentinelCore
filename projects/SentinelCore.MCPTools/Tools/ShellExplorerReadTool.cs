// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ShellExplorerReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying shell and Explorer settings.
/// </summary>
[McpServerToolType]
public sealed class ShellExplorerReadTool
{

    private const string AdvancedKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
    private const string ExplorerKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer";








    [SupportedOSPlatform("windows")]
    private static void ReadKeyValues(RegistryKey root, string keyPath, List<object> results, string[] valueNames)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        foreach (string valueName in valueNames)
        {
            object? value = key.GetValue(valueName);
            if (value is not null)
            {
                results.Add(new { Key = keyPath, Name = valueName, Value = Helpers.CleanValue(value.ToString()) });
            }
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_read_shell_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads common Explorer settings such as hidden files and file extensions.")]
    public async Task<ToolResult> ShellExplorerReadSettingsAsync()
    {
        try
        {
            List<object> results = new();
            ReadKeyValues(Registry.CurrentUser, AdvancedKey, results, ["Hidden", "ShowSuperHidden", "HideFileExt", "LaunchTo"]);
            ReadKeyValues(Registry.CurrentUser, ExplorerKey, results, ["EnableAutoTray", "ShellState"]);

            return ToolResult.Ok(results, "ShellExplorerReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "ShellExplorerReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_list_taskbar_pinned", ReadOnly = true, Destructive = false)]
    [Description("Lists pinned items in the Windows taskbar Quick Launch/User Pinned path.")]
    public async Task<ToolResult> ShellTaskbarPinnedListAsync()
    {
        try
        {
            string pinnedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");
            if (!Directory.Exists(pinnedPath))
            {
                return ToolResult.Fail($"Taskbar pinned path not found: {pinnedPath}", "ShellExplorerReadTool");
            }

            List<string> pinnedItems = new();
            foreach (string file in Directory.GetFiles(pinnedPath, "*.lnk"))
            {
                pinnedItems.Add(Helpers.CleanValue(Path.GetFileName(file)));
            }

            return ToolResult.Ok(pinnedItems, "ShellExplorerReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "ShellExplorerReadTool");
        }
    }
}
