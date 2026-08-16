// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BatteryReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Management;
using System.Text;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying battery and power status using CIM (Win32_Battery) and system power calls.
/// </summary>
public sealed class BatteryReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying battery and power status.";
    public override string Name { get; } = "Battery_Read";








    [Description("Lists battery status for the system using Win32_Battery.")]
    public Task<ToolResult> battery_list()
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Name, Description, EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime, PowerManagementCapabilities FROM Win32_Battery");
            foreach (ManagementObject battery in searcher.Get())
                results.Add(new
                {
                        Name = battery["Name"]?.ToString(),
                        Description = battery["Description"]?.ToString(),
                        EstimatedChargeRemaining = battery["EstimatedChargeRemaining"]?.ToString(),
                        BatteryStatus = battery["BatteryStatus"]?.ToString(),
                        EstimatedRunTime = battery["EstimatedRunTime"]?.ToString()
                });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Battery listing failed: {ex.Message}"));
        }
    }








    [Description("Reads power plan settings related to battery from the power WMI namespace.")]
    public Task<ToolResult> battery_read_power_settings()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2\\power", "SELECT InstanceId, ElementName, Value FROM Win32_PowerSettingDataIndex");
            foreach (ManagementObject setting in searcher.Get())
            {
                string? name = setting["ElementName"]?.ToString() ?? string.Empty;
                if (name.Contains("battery", StringComparison.OrdinalIgnoreCase) || name.Contains("low", StringComparison.OrdinalIgnoreCase) || name.Contains("critical", StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"InstanceId={setting["InstanceId"]}, Name={name}, Value={setting["Value"]}");
            }

            return Task.FromResult(ToolResult.Ok(sb.Length == 0 ? "No battery-specific power settings found." : sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Battery power settings read failed: {ex.Message}"));
        }
    }
}