// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         RegistryReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows registry keys and values.
/// </summary>
public sealed class RegistryReadTool : AITool
{

    private static string FormatRegistryValue(object value, RegistryValueKind kind)
    {
        return kind switch
        {
                RegistryValueKind.MultiString => string.Join("|", (string[])value),
                RegistryValueKind.Binary => Convert.ToHexString((byte[])value),
                _ => value.ToString() ?? string.Empty
        };
    }








    private static RegistryKey? GetHiveRoot(string hive)
    {
        return hive.ToUpperInvariant() switch
        {
                "HKLM" => Registry.LocalMachine,
                "HKCU" => Registry.CurrentUser,
                "HKCR" => Registry.ClassesRoot,
                "HKU" => Registry.Users,
                "HKCC" => Registry.CurrentConfig,
                _ => null
        };
    }








    [Description("Lists subkey names and value names under the specified registry key path.")]
    public Task<ToolResult> registry_list_key([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The key path within the hive.")] string keyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hive))
            {
                return Task.FromResult(ToolResult.FailureResult("hive is required."));
            }

            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return Task.FromResult(ToolResult.FailureResult("keyPath is required."));
            }

            RegistryKey? root = GetHiveRoot(hive);
            if (root is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Unknown registry hive: {hive}"));
            }

            using RegistryKey? key = root.OpenSubKey(keyPath, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Registry key not found: {hive}\\{keyPath}"));
            }

            string[] subkeys = key.GetSubKeyNames();
            string[] values = key.GetValueNames();
            StringBuilder sb = new();
            sb.AppendLine($"Hive={hive}");
            sb.AppendLine($"Path={keyPath}");
            sb.AppendLine("SubKeys:");
            foreach (string sub in subkeys) sb.AppendLine($"  {sub}");

            sb.AppendLine("Values:");
            foreach (string val in values)
            {
                string displayName = string.IsNullOrEmpty(val) ? "(Default)" : val;
                sb.AppendLine($"  {displayName}");
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Registry list failed: {ex.Message}"));
        }
    }








    [Description("Reads a registry value from the specified key path. Use hive names such as HKLM, HKCU, HKCR, HKU, HKCC.")]
    public Task<ToolResult> registry_read_value([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The key path within the hive, e.g. SOFTWARE\\Microsoft\\Windows\\CurrentVersion.")] string keyPath, [Description("The name of the value to read. Use null or empty to read the default value.")] string? valueName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hive))
            {
                return Task.FromResult(ToolResult.FailureResult("hive is required."));
            }

            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return Task.FromResult(ToolResult.FailureResult("keyPath is required."));
            }

            RegistryKey? root = GetHiveRoot(hive);
            if (root is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Unknown registry hive: {hive}"));
            }

            using RegistryKey? key = root.OpenSubKey(keyPath, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Registry key not found: {hive}\\{keyPath}"));
            }

            string actualValueName = string.IsNullOrWhiteSpace(valueName) ? string.Empty : valueName;
            object? value = key.GetValue(actualValueName);
            if (value is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Registry value not found: {actualValueName} under {hive}\\{keyPath}"));
            }

            RegistryValueKind kind = key.GetValueKind(actualValueName);
            string result = $"Kind={kind}, Value={FormatRegistryValue(value, kind)}";
            return Task.FromResult(ToolResult.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Registry read failed: {ex.Message}"));
        }
    }
}