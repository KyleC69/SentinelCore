// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         WindowsUpdateReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Update settings.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class WindowsUpdateReadTool
{

    private const string WindowsUpdateAutoUpdateKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
    private const string WindowsUpdatePolicyKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate";








    private void ReadKeyValues(string keyPath, StringBuilder sb)
    {
        using RegistryKey? key = RegistryHelper.OpenKey("HKLM", keyPath);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"[{keyPath}]");
        foreach (RegistryHelper.RegistryValueRecord v in RegistryHelper.ReadValues(key)) sb.AppendLine($"  {v.Name}={v.Value}");
    }








    [McpServerTool(Name = "sc_configuration_read_update_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads Windows Update policy settings from the registry.")]
    public async Task<ToolResult> WindowsUpdateReadSettingsAsync()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(WindowsUpdatePolicyKey, sb);
            ReadKeyValues(WindowsUpdateAutoUpdateKey, sb);

            return ToolResult.Ok(sb.Length == 0 ? "No Windows Update policy settings configured." : sb.ToString(), "WindowsUpdateReadTool");

        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Windows Update settings read");
        }
    }
}
