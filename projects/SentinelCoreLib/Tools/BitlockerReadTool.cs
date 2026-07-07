// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         BitlockerReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying BitLocker volume encryption status via BitLocker WMI v2.
/// </summary>
public sealed class BitlockerReadTool : AITool
{
    [Description("Lists BitLocker-protected volumes and their encryption status.")]
    public Task<ToolResult> bitlocker_list_volumes()
    {
        try
        {
            var results = new List<object>();
            using ManagementObjectSearcher searcher = new(@"root\cimv2\security\MicrosoftVolumeEncryption", "SELECT DeviceID, ProtectionStatus, EncryptionMethod, ConversionStatus FROM Win32_EncryptableVolume");
            foreach (ManagementObject volume in searcher.Get())
                results.Add(new { DeviceID = volume["DeviceID"]?.ToString(), ProtectionStatus = volume["ProtectionStatus"]?.ToString(), EncryptionMethod = volume["EncryptionMethod"]?.ToString(), ConversionStatus = volume["ConversionStatus"]?.ToString() });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"BitLocker volume listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the BitLocker metadata / key protector types for a specific volume.")]
    public Task<ToolResult> bitlocker_read_volume([Description("The device ID of the encryptable volume, e.g. \\\\?\\\\Volume{GUID}\\\\.")] string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Task.FromResult(ToolResult.FailureResult("deviceId is required."));
            }

            string escaped = deviceId.Replace("\\", "\\\\");
            string query = $"SELECT * FROM Win32_EncryptableVolume WHERE DeviceID='{escaped}'";
            StringBuilder sb = new();
            using ManagementObjectSearcher searcher = new(@"root\cimv2\security\MicrosoftVolumeEncryption", query);
            foreach (ManagementObject volume in searcher.Get())
            {
                foreach (PropertyData? property in volume.Properties) sb.AppendLine($"{property.Name}={property.Value}");
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.Length == 0 ? "Volume not found." : sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"BitLocker volume read failed: {ex.Message}"));
        }
    }
}