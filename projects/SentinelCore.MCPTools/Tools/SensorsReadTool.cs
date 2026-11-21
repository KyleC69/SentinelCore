// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         SensorsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating sensor devices using the Windows Sensor API surface (via CIM).
/// </summary>
[McpServerToolType]
public sealed class SensorsReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_sensors", ReadOnly = true, Destructive = false)]
    [Description("Lists sensor devices via CIM Win32_PnPEntity matching common sensor class names.")]
    public async Task<ToolResult> SensorListDevicesAsync()
    {
        try
        {
            List<object> results = new();
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

            return ToolResult.Ok(results, "SensorsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "SensorsReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_location_sensor", ReadOnly = true, Destructive = false)]
    [Description("Reads the Windows sensor permissions / location service status from CIM.")]
    public async Task<ToolResult> SensorReadLocationServiceAsync()
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Status FROM Win32_Service WHERE Name='lfsvc'");
            foreach (ManagementObject service in searcher.Get())
                results.Add(new { Service = "lfsvc", Status = Helpers.CleanValue(service["Status"]?.ToString()) });

            return results.Count == 0 ? ToolResult.Fail("Location service not found.", "SensorsReadTool") : ToolResult.Ok(results, "SensorsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "SensorsReadTool");
        }
    }
}
