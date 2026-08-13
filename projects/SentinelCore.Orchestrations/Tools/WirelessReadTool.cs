// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WirelessReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating wireless network interfaces and saved profiles.
///     Uses CIM (not legacy WMI) via Microsoft.Management.Infrastructure where possible; falls back to System.Management
///     for WQL queries.
/// </summary>
public sealed class WirelessReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for enumerating wireless network interfaces and saved profiles.";
    public override string Name { get; } = "Wireless_Read";








    [Description("Lists wireless network interfaces on the system using CIM/MSNdis classes.")]
    public Task<ToolResult> wireless_list_interfaces()
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\StandardCimv2", "SELECT InstanceID, Name, InterfaceDescription, State, Active FROM MSFT_NetAdapter WHERE InterfaceDescription LIKE '%Wireless%' OR InterfaceDescription LIKE '%Wi-Fi%'");
            foreach (ManagementObject adapter in searcher.Get())
                results.Add(new
                {
                        InstanceID = adapter["InstanceID"]?.ToString(),
                        Name = adapter["Name"]?.ToString(),
                        InterfaceDescription = adapter["InterfaceDescription"]?.ToString(),
                        State = adapter["State"]?.ToString(),
                        Active = adapter["Active"]?.ToString()
                });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Wireless interface listing failed: {ex.Message}"));
        }
    }








    [Description("Lists saved Wi-Fi profiles using netsh as a read-only native command invocation.")]
    public Task<ToolResult> wireless_list_profiles()
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                    FileName = "netsh",
                    Arguments = "wlan show profiles",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return Task.FromResult(ToolResult.Fail("Failed to start netsh."));
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return Task.FromResult(ToolResult.Fail($"netsh failed: {stderr}"));
            }

            return Task.FromResult(ToolResult.Ok(stdout));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Wireless profile listing failed: {ex.Message}"));
        }
    }
}