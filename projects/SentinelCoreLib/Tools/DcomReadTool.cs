// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         DcomReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying DCOM application configuration through WMI.
/// </summary>
public sealed class DcomReadTool : AITool
{
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

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"DCOM application query failed: {ex.Message}"));
        }
    }








    [Description("Reads DCOM AppID settings from the registry under HKLM\\SOFTWARE\\Classes\\AppID.")]
    public Task<ToolResult> dcom_read_appid_settings([Description("The AppID GUID to inspect, including braces.")] string appId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                return Task.FromResult(ToolResult.FailureResult("appId is required."));
            }

            string keyPath = $"SOFTWARE\\Classes\\AppID\\{appId}";
            using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"AppID registry key not found: {keyPath}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"AppID={appId}");
            foreach (string valueName in key.GetValueNames())
            {
                string displayName = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName;
                sb.AppendLine($"{displayName}={key.GetValue(valueName)}");
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"DCOM AppID read failed: {ex.Message}"));
        }
    }
}