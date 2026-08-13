// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RegistryReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows registry keys and values.
/// </summary>
public sealed class RegistryReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying Windows registry keys and values.";
    public override string Name { get; } = "Registry_Read";








    private static string FormatRegistryValue(object value, RegistryValueKind kind)
    {
        return kind switch
        {
                RegistryValueKind.MultiString => string.Join("|", (string[])value),
                RegistryValueKind.Binary => Convert.ToHexString((byte[])value),
                _ => value?.ToString() ?? string.Empty
        };
    }








    [Description("Registry tool to get the hive root")]
    private static RegistryKey? GetHiveRoot([Description("The hive to get the root of")] string hive)
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








    /// <summary>
    ///     Lists the subkey names and value names under the specified registry key path.
    /// </summary>
    /// <param name="hive">
    ///     The registry hive abbreviation (e.g., HKLM, HKCU, HKCR, HKU, HKCC).
    /// </param>
    /// <param name="keyPath">
    ///     The path of the registry key within the specified hive.
    /// </param>
    /// <returns>
    ///     A <see cref="ToolResult" /> containing the list of subkeys and values if successful,
    ///     or an error message if the operation fails.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="hive" /> or <paramref name="keyPath" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="Exception">
    ///     Thrown if an unexpected error occurs while accessing the registry.
    /// </exception>
    [Description("Queries the registry and returns Lists subkey names and value names under the specified registry key path.")]
    public Task<ToolResult> registry_list_key([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The key path within the hive.")] string keyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hive)) return Task.FromResult(ToolResult.Fail("hive is required."));

            if (string.IsNullOrWhiteSpace(keyPath))
                return Task.FromResult(ToolResult.Fail("keyPath is required."));

            RegistryKey? root = GetHiveRoot(hive);
            if (root is null)
            {
                return Task.FromResult(ToolResult.Fail($"Unknown registry hive: {hive}"));
            }

            using RegistryKey? key = root.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail($"Registry key not found: {hive}\\{keyPath}"));
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

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Registry list failed: {ex.Message}"));
        }
    }








    [Description("Reads a registry value from the specified key path. Use hive names such as HKLM, HKCU, HKCR, HKU, HKCC.")]
    public Task<ToolResult> registry_read_value([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The path within the hive to return values from, e.g. SOFTWARE\\Microsoft\\Windows\\CurrentVersion.")] string keyPath, [Description("The name of the value to read. ")] string? valueName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hive))
            {
                return Task.FromResult(ToolResult.Fail("hive is required."));
            }

            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return Task.FromResult(ToolResult.Fail("keyPath is required."));
            }

            RegistryKey? root = GetHiveRoot(hive);
            if (root is null)
            {
                return Task.FromResult(ToolResult.Fail($"Unknown registry hive: {hive}"));
            }

            using RegistryKey? key = root.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail($"Registry key not found: {hive}\\{keyPath}"));
            }

            string actualValueName = string.IsNullOrWhiteSpace(valueName) ? string.Empty : valueName;
            object? value = key.GetValue(actualValueName);
            if (value is null)
            {
                return Task.FromResult(ToolResult.Fail($"Registry value not found: {actualValueName} under {hive}\\{keyPath}"));
            }

            RegistryValueKind kind = key.GetValueKind(actualValueName);
            string result = $"Kind={kind}, Value={FormatRegistryValue(value, kind)}";
            return Task.FromResult(ToolResult.Ok(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Registry read failed: {ex.Message}"));
        }
    }
}