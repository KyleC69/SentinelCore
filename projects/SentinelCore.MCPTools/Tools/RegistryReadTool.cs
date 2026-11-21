// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RegistryReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows registry keys and values.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class RegistryReadTool
{

    /// <summary>
    ///     Lists the subkey names and value names under the specified registry key path.
    /// </summary>
    /// <param name="hive">Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).</param>
    /// <param name="keyPath">The key path within the hive.</param>
    /// <param name="maxRecords">Maximum number of subkeys and values to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed key listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_list_registry_keys", ReadOnly = true, Destructive = false)]
    [Description("Queries the registry and returns Lists subkey names and value names under the specified registry key path.")]
    public async Task<ToolResult> RegistryListKeyAsync([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The key path within the hive.")] string keyPath, [Description("Maximum number of subkeys and values to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? validationResult = InputValidator.ValidateRegistryHive(hive);
            if (validationResult is not null)
            {
                return validationResult;
            }

            validationResult = InputValidator.ValidateRequired(keyPath, "keyPath");
            if (validationResult is not null)
            {
                return validationResult;
            }

            validationResult = InputValidator.ValidateMaxRecords(maxRecords);
            if (validationResult is not null)
            {
                return validationResult;
            }

            RegistryKey? root = RegistryHelper.GetHiveRoot(hive);
            if (root is null)
            {
                return ToolResult.Fail($"Unknown registry hive: {hive}. Supported hives: HKLM, HKCU, HKCR, HKU, HKCC.", "Registry list key");
            }

            RegistryKeyListingRecord? listing = await Task.Run(() =>
                    {
                        using RegistryKey? key = root.OpenSubKey(keyPath, false);
                        if (key is null)
                        {
                            return null;
                        }

                        List<string> subkeys = key.GetSubKeyNames().Take(maxRecords).ToList();
                        List<string> values = key.GetValueNames().Select(v => string.IsNullOrEmpty(v) ? "(Default)" : v).Take(maxRecords).ToList();

                        return new RegistryKeyListingRecord(hive, keyPath, subkeys, values);
                    })
                    .ConfigureAwait(false);

            if (listing is null)
            {
                return ToolResult.Fail($"Registry key not found: {hive}\\{keyPath}", "Registry list key");
            }

            return ToolResult.Ok(listing, $"Registry key listing for {hive}\\{keyPath}");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Registry list key for {hive}\\{keyPath}");
        }
    }








    /// <summary>
    ///     Reads a registry value from the specified key path.
    /// </summary>
    /// <param name="hive">Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).</param>
    /// <param name="keyPath">The path within the hive to return values from.</param>
    /// <param name="valueName">The name of the value to read. Null reads the default value.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed value record.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_registry_value", ReadOnly = true, Destructive = false)]
    [Description("Reads a registry value from the specified key path. Use hive names such as HKLM, HKCU, HKCR, HKU, HKCC.")]
    public async Task<ToolResult> RegistryReadValueAsync([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The path within the hive to return values from, e.g. SOFTWARE\\Microsoft\\Windows\\CurrentVersion.")] string keyPath, [Description("The name of the value to read.")] string? valueName = null)
    {
        try
        {
            ToolResult? validationResult = InputValidator.ValidateRegistryHive(hive);
            if (validationResult is not null)
            {
                return validationResult;
            }

            validationResult = InputValidator.ValidateRequired(keyPath, "keyPath");
            if (validationResult is not null)
            {
                return validationResult;
            }

            RegistryKey? root = RegistryHelper.GetHiveRoot(hive);
            if (root is null)
            {
                return ToolResult.Fail($"Unknown registry hive: {hive}. Supported hives: HKLM, HKCU, HKCR, HKU, HKCC.", "Registry read value");
            }

            RegistryValueReadRecord? record = await Task.Run(() =>
                    {
                        using RegistryKey? key = root.OpenSubKey(keyPath, false);
                        if (key is null)
                        {
                            return null;
                        }

                        string actualValueName = string.IsNullOrWhiteSpace(valueName) ? string.Empty : valueName;
                        object? value = key.GetValue(actualValueName);
                        if (value is null)
                        {
                            return null;
                        }

                        RegistryValueKind kind = key.GetValueKind(actualValueName);
                        string displayName = string.IsNullOrEmpty(actualValueName) ? "(Default)" : actualValueName;
                        return new RegistryValueReadRecord(hive, keyPath, displayName, kind.ToString(), RegistryHelper.FormatRegistryValue(value, kind));
                    })
                    .ConfigureAwait(false);

            if (record is null)
            {
                return ToolResult.Fail($"Registry value not found: {valueName ?? "(Default)"} under {hive}\\{keyPath}", "Registry read value");
            }

            return ToolResult.Ok(record, $"Registry value read for {hive}\\{keyPath}\\{record.ValueName}");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Registry read value for {hive}\\{keyPath}");
        }
    }








    /// <summary>
    ///     A single registry subkey/value listing record.
    /// </summary>
    /// <param name="Hive">The hive abbreviation.</param>
    /// <param name="Path">The key path within the hive.</param>
    /// <param name="SubKeys">The subkey names.</param>
    /// <param name="Values">The value names.</param>
    public sealed record RegistryKeyListingRecord(string Hive, string Path, IReadOnlyList<string> SubKeys, IReadOnlyList<string> Values);





    /// <summary>
    ///     A single registry value read record.
    /// </summary>
    /// <param name="Hive">The hive abbreviation.</param>
    /// <param name="Path">The key path within the hive.</param>
    /// <param name="ValueName">The value name, or "(Default)".</param>
    /// <param name="Kind">The registry value kind.</param>
    /// <param name="Value">The formatted value data.</param>
    public sealed record RegistryValueReadRecord(string Hive, string Path, string ValueName, string Kind, string Value);
}
