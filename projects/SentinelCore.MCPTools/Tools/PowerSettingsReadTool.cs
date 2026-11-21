// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PowerSettingsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows power plans and settings.
/// </summary>
[McpServerToolType]
public sealed class PowerSettingsReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_power_plans", ReadOnly = true, Destructive = false)]
    [Description("Lists active and available power plans using WMI.")]
    public async Task<ToolResult> PowerListPlansAsync()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2\\power", "SELECT InstanceId, ElementName, IsActive FROM Win32_PowerPlan");
            foreach (ManagementObject plan in searcher.Get())
            {
                string instanceId = plan["InstanceId"]?.ToString() ?? string.Empty;
                string name = plan["ElementName"]?.ToString() ?? string.Empty;
                string isActive = plan["IsActive"]?.ToString() ?? string.Empty;
                sb.AppendLine($"InstanceId={instanceId}, Name={name}, IsActive={isActive}");
            }

            return ToolResult.Ok(sb.ToString(), "PowerSettingsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PowerSettingsReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_power_settings", ReadOnly = true, Destructive = false)]
    [Description("Lists power settings for the active power plan.")]
    public async Task<ToolResult> PowerListSettingsAsync()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2\\power", "SELECT InstanceId, ElementName, SettingIndexValue FROM Win32_PowerSettingDataIndex");
            foreach (ManagementObject setting in searcher.Get())
            {
                string instanceId = setting["InstanceId"]?.ToString() ?? string.Empty;
                string name = setting["ElementName"]?.ToString() ?? string.Empty;
                string value = setting["SettingIndexValue"]?.ToString() ?? string.Empty;
                sb.AppendLine($"InstanceId={instanceId}, Name={name}, Value={value}");
            }

            return ToolResult.Ok(sb.ToString(), "PowerSettingsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PowerSettingsReadTool");
        }
    }
}
