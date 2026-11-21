// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         BatteryReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying battery and power status using CIM (Win32_Battery) and system power calls.
/// </summary>
[McpServerToolType]
public sealed class BatteryReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_batteries", ReadOnly = true, Destructive = false)]
    [Description("Lists battery status for the system using Win32_Battery.")]
    public async Task<ToolResult> BatteryListAsync([Description("Maximum number of batteries to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Name, Description, EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime, PowerManagementCapabilities FROM Win32_Battery");
            foreach (ManagementObject battery in searcher.Get())
            {
                if (results.Count >= maxRecords)
                {
                    break;
                }

                results.Add(new
                {
                        Name = battery["Name"]?.ToString(),
                        Description = battery["Description"]?.ToString(),
                        EstimatedChargeRemaining = battery["EstimatedChargeRemaining"]?.ToString(),
                        BatteryStatus = battery["BatteryStatus"]?.ToString(),
                        EstimatedRunTime = battery["EstimatedRunTime"]?.ToString()
                });
            }

            return ToolResult.Ok(results, "BatteryReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BatteryReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_battery_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads power plan settings related to battery from the power WMI namespace.")]
    public async Task<ToolResult> BatteryReadPowerSettingsAsync()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2\\power", "SELECT InstanceId, ElementName, SettingIndexValue FROM Win32_PowerSettingDataIndex");
            foreach (ManagementObject setting in searcher.Get())
            {
                string? name = setting["ElementName"]?.ToString() ?? string.Empty;
                if (name.Contains("battery", StringComparison.OrdinalIgnoreCase) || name.Contains("low", StringComparison.OrdinalIgnoreCase) || name.Contains("critical", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"InstanceId={setting["InstanceId"]}, Name={name}, Value={setting["SettingIndexValue"]}");
                }
            }

            return sb.Length == 0 ? ToolResult.Ok("No battery-specific power settings found.", "BatteryReadTool") : ToolResult.Ok(sb.ToString(), "BatteryReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BatteryReadTool");
        }
    }
}
