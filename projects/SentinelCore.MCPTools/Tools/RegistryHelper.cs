// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RegistryHelper.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

using Microsoft.Win32;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Shared utility for reading Windows registry keys and values.
///     Centralizes the hive abbreviation mapping, key opening, and value formatting
///     so that all registry-based tools use the same logic.
///     All blocking registry I/O is wrapped in <see cref="Task.Run" /> per spec §4.2.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class RegistryHelper
{

    /// <summary>
    ///     Formats a registry value based on its <see cref="RegistryValueKind" />.
    ///     MultiString values are joined with "|"; Binary values are hex-encoded;
    ///     all others use <see cref="object.ToString" />.
    /// </summary>
    /// <param name="value">The raw registry value.</param>
    /// <param name="kind">The registry value kind.</param>
    /// <returns>A formatted string representation of the value.</returns>
    internal static string FormatRegistryValue(object value, RegistryValueKind kind)
    {
        return kind switch
        {
                RegistryValueKind.MultiString => string.Join("|", (string[])value),
                RegistryValueKind.Binary => Convert.ToHexString((byte[])value),
                _ => value?.ToString() ?? string.Empty
        };
    }








    /// <summary>
    ///     Maps a registry hive abbreviation (e.g., HKLM, HKCU) to the corresponding <see cref="RegistryKey" /> root.
    /// </summary>
    /// <param name="hive">The hive abbreviation. Supported values: HKLM, HKCU, HKCR, HKU, HKCC.</param>
    /// <returns>The <see cref="RegistryKey" /> root, or <c>null</c> if the abbreviation is not recognized.</returns>
    internal static RegistryKey? GetHiveRoot(string hive)
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
    ///     Opens a registry key for reading using the specified hive abbreviation and key path.
    /// </summary>
    /// <param name="hive">The hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).</param>
    /// <param name="keyPath">The path of the registry key within the specified hive.</param>
    /// <returns>The opened <see cref="RegistryKey" />, or <c>null</c> if the key does not exist.</returns>
    internal static RegistryKey? OpenKey(string hive, string keyPath)
    {
        RegistryKey? root = GetHiveRoot(hive);
        if (root is null)
        {
            return null;
        }

        return root.OpenSubKey(keyPath, false);
    }








    /// <summary>
    ///     Reads the ACL (access control list) of a registry key using the .NET API.
    /// </summary>
    /// <param name="hive">The hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).</param>
    /// <param name="keyPath">The path of the registry key within the specified hive.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed ACL payload, or a failure result.</returns>
    internal static async Task<ToolResult> ReadAclAsync(string hive, string keyPath)
    {
        try
        {
            ToolResult? hiveValidation = InputValidator.ValidateRegistryHive(hive);
            if (hiveValidation is not null)
            {
                return hiveValidation;
            }

            ToolResult? pathValidation = InputValidator.ValidateRequired(keyPath, "keyPath");
            if (pathValidation is not null)
            {
                return pathValidation;
            }

            RegistryKey? root = GetHiveRoot(hive);
            if (root is null)
            {
                return ToolResult.Fail($"Unknown registry hive: {hive}. Supported hives: HKLM, HKCU, HKCR, HKU, HKCC.", "Registry ACL read");
            }

            RegistryAclResult? acl = await Task.Run(() =>
                    {
                        using RegistryKey? key = root.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadSubTree);
                        if (key is null)
                        {
                            return null;
                        }

                        RegistrySecurity security = key.GetAccessControl();
                        IdentityReference? owner = security.GetOwner(typeof(NTAccount));
                        IdentityReference? group = security.GetGroup(typeof(NTAccount));

                        List<RegistryAccessRuleRecord> rules = new();
                        foreach (RegistryAccessRule rule in security.GetAccessRules(true, true, typeof(NTAccount)).Cast<RegistryAccessRule>())
                        {
                            rules.Add(new RegistryAccessRuleRecord(rule.IdentityReference.Value, rule.RegistryRights.ToString(), rule.AccessControlType.ToString(), rule.InheritanceFlags.ToString(), rule.PropagationFlags.ToString()));
                        }

                        return new RegistryAclResult($"{hive}\\{keyPath}", owner?.Value ?? string.Empty, group?.Value ?? string.Empty, rules);
                    })
                    .ConfigureAwait(false);

            if (acl is null)
            {
                return ToolResult.Fail($"Registry key not found: {hive}\\{keyPath}", "Registry ACL read");
            }

            return ToolResult.Ok(acl, $"Registry ACL read for {hive}\\{keyPath}");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Registry ACL read for {hive}\\{keyPath}");
        }
    }








    /// <summary>
    ///     Reads all value names and their data from a registry key as typed records.
    /// </summary>
    /// <param name="key">The registry key to read values from.</param>
    /// <returns>The list of value records.</returns>
    internal static List<RegistryValueRecord> ReadValues(RegistryKey key)
    {
        List<RegistryValueRecord> records = new();
        foreach (string valueName in key.GetValueNames())
        {
            string displayName = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName;
            object? value = key.GetValue(valueName);
            RegistryValueKind kind = key.GetValueKind(valueName);
            records.Add(new RegistryValueRecord(displayName, FormatRegistryValue(value ?? string.Empty, kind), kind.ToString()));
        }

        return records;
    }








    /// <summary>
    ///     Reads all value names and their data from a registry key asynchronously as typed records.
    /// </summary>
    /// <param name="key">The registry key to read values from.</param>
    /// <returns>A task producing the list of value records.</returns>
    internal static Task<List<RegistryValueRecord>> ReadValuesAsync(RegistryKey key)
    {
        return Task.Run(() => ReadValues(key));
    }








    /// <summary>
    ///     A single registry value record returned by <see cref="ReadValuesAsync" />.
    /// </summary>
    /// <param name="Name">The value name, or "(Default)" for the default value.</param>
    /// <param name="Value">The formatted value data.</param>
    /// <param name="Kind">The registry value kind.</param>
    public sealed record RegistryValueRecord(string Name, string Value, string Kind);





    /// <summary>
    ///     A single registry access rule record returned by <see cref="ReadAclAsync" />.
    /// </summary>
    /// <param name="Identity">The account the rule applies to.</param>
    /// <param name="Rights">The registry rights granted or denied.</param>
    /// <param name="Type">Allow or Deny.</param>
    /// <param name="Inheritance">Inheritance flags.</param>
    /// <param name="Propagation">Propagation flags.</param>
    public sealed record RegistryAccessRuleRecord(string Identity, string Rights, string Type, string Inheritance, string Propagation);





    /// <summary>
    ///     The ACL payload for a registry key.
    /// </summary>
    /// <param name="Path">The full hive-qualified key path.</param>
    /// <param name="Owner">The key owner.</param>
    /// <param name="Group">The key group.</param>
    /// <param name="AccessRules">The access rules.</param>
    public sealed record RegistryAclResult(string Path, string Owner, string Group, IReadOnlyList<RegistryAccessRuleRecord> AccessRules);
}