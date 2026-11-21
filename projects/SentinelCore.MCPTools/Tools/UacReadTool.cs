// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         UacReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Principal;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying UAC and token elevation state.
/// </summary>
[McpServerToolType]
public sealed class UacReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_uac_policy", ReadOnly = true, Destructive = false)]
    [Description("Reads UAC policy settings from the registry.")]
    public async Task<ToolResult> UacReadSettingsAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
            if (key is null)
            {
                return ToolResult.Fail("UAC registry key not found.", "UacReadSettingsTool");
            }

            foreach (string valueName in key.GetValueNames())
            {
                results.Add(new { Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
            }

            return ToolResult.Ok(results, "UacReadSettingsTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "UacReadSettingsTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_token_elevation", ReadOnly = true, Destructive = false)]
    [Description("Reports whether the current process token is elevated.")]
    public async Task<ToolResult> UacReadTokenElevationAsync()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            bool elevated = !identity.IsSystem && isAdmin;
            return ToolResult.Ok(new { IsElevated = elevated, IsAdministrator = isAdmin }, "UacReadTokenElevationTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "UacReadTokenElevationTool");
        }
    }
}
