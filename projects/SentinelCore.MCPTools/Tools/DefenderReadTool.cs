// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         DefenderReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Microsoft Defender configuration and status.
///     Uses Windows Security Center API surface via CIM and registry.
/// </summary>
[McpServerToolType]
public sealed class DefenderReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_defender_config", ReadOnly = true, Destructive = false)]
    [Description("Reads Defender exclusion and general configuration registry entries.")]
    public async Task<ToolResult> DefenderReadRegistryConfigAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender", false);
            if (key is null)
            {
                return ToolResult.Fail("Windows Defender registry key not found.", "DefenderReadTool");
            }

            foreach (string valueName in key.GetValueNames())
            {
                results.Add(new { Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
            }

            return ToolResult.Ok(results, "DefenderReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "DefenderReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_defender_status", ReadOnly = true, Destructive = false)]
    [Description("Reads Microsoft Defender antivirus status via the Windows Security Center WMI provider.")]
    public async Task<ToolResult> DefenderReadStatusAsync()
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new(@"root\Microsoft\Windows\Defender", "SELECT * FROM MSFT_MpComputerStatus");
            foreach (ManagementObject status in searcher.Get())
            {
                Dictionary<string, object?> record = new();
                foreach (PropertyData? property in status.Properties) record[property.Name] = property.Value;

                results.Add(record);
            }

            return ToolResult.Ok(results, "DefenderReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "DefenderReadTool");
        }
    }
}
