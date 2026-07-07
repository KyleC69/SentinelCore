// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SensorsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for enumerating sensor devices using the Windows Sensor API surface (via CIM).
/// </summary>
public sealed class SensorsReadTool : AITool
{
    [Description("Lists sensor devices via CIM Win32_PnPEntity matching common sensor class names.")]
    public Task<ToolResult> sensor_list_devices()
    {
        try
        {
            var results = new List<object>();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT DeviceID, Name, Status, PNPClass, Manufacturer FROM Win32_PnPEntity WHERE PNPClass LIKE '%Sensor%' OR Name LIKE '%Sensor%'");
            foreach (ManagementObject device in searcher.Get())
                results.Add(new
                {
                        DeviceID = device["DeviceID"]?.ToString(),
                        Name = device["Name"]?.ToString(),
                        Status = device["Status"]?.ToString(),
                        PNPClass = device["PNPClass"]?.ToString(),
                        Manufacturer = device["Manufacturer"]?.ToString()
                });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Sensor device listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the Windows sensor permissions / location service status from CIM.")]
    public Task<ToolResult> sensor_read_location_service()
    {
        try
        {
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Status FROM Win32_Service WHERE Name='lfsvc'");
            foreach (ManagementObject service in searcher.Get()) sb.AppendLine($"LocationService(lfsvc) Status={service["Status"]}");

            return Task.FromResult(ToolResult.SuccessResult(sb.Length == 0 ? "Location service not found." : sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Sensor location service read failed: {ex.Message}"));
        }
    }
}