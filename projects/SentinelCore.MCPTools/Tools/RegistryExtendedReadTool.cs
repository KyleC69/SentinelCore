// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RegistryExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying extended registry information: ACLs on registry keys
///     and COM class registrations for COM hijacking detection.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class RegistryExtendedReadTool
{

    /// <summary>
    ///     Lists COM class registrations from the registry for COM hijacking detection.
    /// </summary>
    /// <param name="maxRecords">Maximum number of classes to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing typed COM class records.</returns>
    [McpServerTool(Name = "sc_forensics_list_com_classes", ReadOnly = true, Destructive = false)]
    [Description("Lists COM class registrations from the registry for COM hijacking detection.")]
    public async Task<ToolResult> ComListClassesAsync([Description("Maximum number of classes to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxRecordsValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxRecordsValidation is not null)
            {
                return maxRecordsValidation;
            }

            List<ComClassRecord> results = await Task.Run(() =>
                    {
                        List<ComClassRecord> records = new();
                        using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
                        using RegistryKey? clsidKey = baseKey.OpenSubKey(@"CLSID", false);
                        if (clsidKey is null)
                        {
                            return records;
                        }

                        foreach (string clsid in clsidKey.GetSubKeyNames())
                        {
                            if (records.Count >= maxRecords)
                            {
                                break;
                            }

                            using RegistryKey? classKey = clsidKey.OpenSubKey(clsid, false);
                            if (classKey is null)
                            {
                                continue;
                            }

                            string? inprocPath = null;
                            using (RegistryKey? inprocKey = classKey.OpenSubKey(@"InprocServer32", false))
                            {
                                inprocPath = inprocKey?.GetValue(null)?.ToString();
                            }

                            string? localServer = null;
                            using (RegistryKey? localServerKey = classKey.OpenSubKey(@"LocalServer32", false))
                            {
                                localServer = localServerKey?.GetValue(null)?.ToString();
                            }

                            string? progId = null;
                            using (RegistryKey? progIdKey = classKey.OpenSubKey(@"ProgID", false))
                            {
                                progId = progIdKey?.GetValue(null)?.ToString();
                            }

                            records.Add(new ComClassRecord(clsid, progId ?? string.Empty, inprocPath ?? string.Empty, localServer ?? string.Empty));
                        }

                        return records;
                    })
                    .ConfigureAwait(false);

            return ToolResult.Ok(results, $"Enumerated {results.Count} COM class registration(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "COM class listing");
        }
    }








    /// <summary>
    ///     Reads the ACL (access control list) of a registry key for permission auditing.
    /// </summary>
    /// <param name="hive">Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).</param>
    /// <param name="keyPath">The key path within the hive.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed ACL payload.</returns>
    [McpServerTool(Name = "sc_configuration_read_registry_acl", ReadOnly = true, Destructive = false)]
    [Description("Reads the ACL (access control list) of a registry key for permission auditing.")]
    public async Task<ToolResult> RegistryReadAclAsync([Description("Registry hive abbreviation (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive, [Description("The key path within the hive.")] string keyPath)
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

            return await RegistryHelper.ReadAclAsync(hive, keyPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Registry ACL read for {hive}\\{keyPath}");
        }
    }








    /// <summary>
    ///     A single COM class registration record.
    /// </summary>
    /// <param name="Clsid">The COM class GUID.</param>
    /// <param name="ProgId">The programmatic identifier.</param>
    /// <param name="InprocServer32">The in-process server path, if registered.</param>
    /// <param name="LocalServer32">The local server path, if registered.</param>
    public sealed record ComClassRecord(string Clsid, string ProgId, string InprocServer32, string LocalServer32);
}
