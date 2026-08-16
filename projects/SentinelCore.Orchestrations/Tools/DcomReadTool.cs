// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DcomReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Management;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying DCOM application configuration through WMI.
/// </summary>
public sealed class DcomReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying DCOM application configuration through WMI.";
    public override string Name { get; } = "DCOM_Read";








    [Description("Lists DCOM applications registered on the system using WMI Win32_DCOMApplication.")]
    public Task<ToolResult> dcom_list_applications()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("SELECT AppID, Name FROM Win32_DCOMApplication");
            foreach (ManagementObject app in searcher.Get())
            {
                string appId = app["AppID"]?.ToString() ?? string.Empty;
                string name = app["Name"]?.ToString() ?? string.Empty;
                sb.AppendLine($"AppID={appId}, Name={name}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"DCOM application query failed: {ex.Message}"));
        }
    }








    [Description("Reads DCOM AppID settings from the registry under HKLM\\SOFTWARE\\Classes\\AppID.")]
    public Task<ToolResult> dcom_read_appid_settings([Description("The AppID GUID to inspect, including braces.")] string appId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(appId))
                return Task.FromResult(ToolResult.Fail("appId is required."));

            string keyPath = $"SOFTWARE\\Classes\\AppID\\{appId}";
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail($"AppID registry key not found: {keyPath}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"AppID={appId}");
            foreach (string valueName in key.GetValueNames())
            {
                string displayName = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName;
                sb.AppendLine($"{displayName}={key.GetValue(valueName)}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"DCOM AppID read failed: {ex.Message}"));
        }
    }
}