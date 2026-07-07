// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PowerSettingsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows power plans and settings.
/// </summary>
public sealed class PowerSettingsReadTool : AITool
{
    [Description("Lists active and available power plans using WMI.")]
    public Task<ToolResult> power_list_plans()
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

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Power plan listing failed: {ex.Message}"));
        }
    }








    [Description("Lists power settings for the active power plan.")]
    public Task<ToolResult> power_list_settings()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2\\power", "SELECT InstanceId, ElementName, Value FROM Win32_PowerSettingDataIndex");
            foreach (ManagementObject setting in searcher.Get())
            {
                string instanceId = setting["InstanceId"]?.ToString() ?? string.Empty;
                string name = setting["ElementName"]?.ToString() ?? string.Empty;
                string value = setting["Value"]?.ToString() ?? string.Empty;
                sb.AppendLine($"InstanceId={instanceId}, Name={name}, Value={value}");
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Power setting listing failed: {ex.Message}"));
        }
    }
}