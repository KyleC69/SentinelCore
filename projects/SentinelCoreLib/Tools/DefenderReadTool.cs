// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         DefenderReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Microsoft Defender configuration and status.
///     Uses Windows Security Center API surface via CIM and registry.
/// </summary>
public sealed class DefenderReadTool : AITool
{

    [Description("Reads Defender exclusion and general configuration registry entries.")]
    public Task<ToolResult> defender_read_registry_config()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender", writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult("Windows Defender registry key not found."));
            }

            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"{valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Defender registry config read failed: {ex.Message}"));
        }
    }








    [Description("Reads Microsoft Defender antivirus status via the Windows Security Center WMI provider.")]
    public Task<ToolResult> defender_read_status()
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

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Defender status read failed: {ex.Message}"));
        }
    }
}