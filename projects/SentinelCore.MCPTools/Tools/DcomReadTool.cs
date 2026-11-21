// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         DcomReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying DCOM application configuration through WMI.
/// </summary>
[McpServerToolType]
public sealed class DcomReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_list_dcom_apps", ReadOnly = true, Destructive = false)]
    [Description("Lists DCOM applications registered on the system using WMI Win32_DCOMApplication.")]
    public async Task<ToolResult> DcomListApplicationsAsync()
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("SELECT AppID, Name FROM Win32_DCOMApplication");
            foreach (ManagementObject app in searcher.Get())
            {
                results.Add(new { AppID = Helpers.CleanValue(app["AppID"]?.ToString()), Name = Helpers.CleanValue(app["Name"]?.ToString()) });
            }

            return ToolResult.Ok(results, "DcomReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "DcomReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_read_dcom_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads DCOM AppID settings from the registry under HKLM\\SOFTWARE\\Classes\\AppID.")]
    public async Task<ToolResult> DcomReadAppidSettingsAsync([Description("The AppID GUID to inspect, including braces.")] string appId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                return ToolResult.Fail("appId is required", "DcomReadTool");
            }

            string keyPath = $"SOFTWARE\\Classes\\AppID\\{appId}";
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return ToolResult.Fail($"AppID registry key not found: {keyPath}", "DcomReadTool");
            }

            List<object> results = new();
            foreach (string valueName in key.GetValueNames())
            {
                string displayName = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName;
                results.Add(new { Name = displayName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
            }

            return ToolResult.Ok(results, "DcomReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "DcomReadTool");
        }
    }
}
