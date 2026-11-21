// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RegDelNullReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for detecting registry values with embedded null characters
///     (RegDelNull equivalent). Such values are invisible to regedit and are a
///     known concealment technique.
///     This tool implements the detection as a managed read-only scan; it never
///     deletes values, unlike the native RegDelNull utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class RegDelNullReadTool
{


    /// <summary>
    ///     Scans a registry key subtree for values whose names or data contain
    ///     embedded null characters. Detection only; no values are deleted.
    /// </summary>
    /// <param name="keyPath">The key path under HKLM to scan, e.g. SOFTWARE.</param>
    /// <param name="maxRecords">Maximum number of findings to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing the null-embedded value findings.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_scan_registry_nulls", ReadOnly = true, Destructive = false)]
    [Description("Scans a registry subtree for values with embedded null characters (RegDelNull detection equivalent). Detection only; never deletes values.")]
    public async Task<ToolResult> RegDelNullScanNullsAsync([Description("The key path under HKLM to scan, e.g. SOFTWARE.")] string keyPath, [Description("Maximum number of findings to return. Defaults to 50.")] int maxRecords = 50)
    {
        ToolResult? pathValidation = InputValidator.ValidateRequired(keyPath, "keyPath");
        if (pathValidation is not null)
        {
            return pathValidation;
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        return await Task.Run(() =>
                {
                    try
                    {
                        List<object> findings = new();
                        using RegistryKey? root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                        ScanKey(root, keyPath, findings, maxRecords, depth: 0);

                        return ToolResult.Ok(new { ScannedPath = $"HKLM\\{keyPath}", Findings = findings, FindingCount = findings.Count }, "Registry null-value scan complete.");
                    }
                    catch (Exception ex)
                    {
                        return ToolResult.Fail(ex.Message, "Registry null-value scan");
                    }
                })
                .ConfigureAwait(false);
    }








    /// <summary>
    ///     Recursively scans a registry key subtree for values whose names or data
    ///     contain embedded null characters.
    /// </summary>
    /// <param name="root">The opened base key.</param>
    /// <param name="relativePath">The key path relative to the base key.</param>
    /// <param name="findings">The findings collection to append to.</param>
    /// <param name="maxRecords">The maximum number of findings to collect.</param>
    /// <param name="depth">The current recursion depth, bounded to prevent runaway traversal.</param>
    private static void ScanKey(RegistryKey root, string keyPath, List<object> findings, int maxRecords, int depth)
    {
        const int maxDepth = 5;

        if (findings.Count >= maxRecords || depth > maxDepth)
        {
            return;
        }

        using RegistryKey? key = root.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        foreach (string valueName in key.GetValueNames())
        {
            if (findings.Count >= maxRecords)
            {
                break;
            }

            bool nameHasNull = valueName.Contains('\0');
            object? data = key.GetValue(valueName);
            bool dataHasNull = data is string s && s.Contains('\0');

            if (nameHasNull || dataHasNull)
            {
                findings.Add(new { KeyPath = $"HKLM\\{keyPath}", ValueName = nameHasNull ? "(name contains embedded null)" : valueName, DataHasNull = dataHasNull });
            }
        }

        if (depth < maxDepth)
        {
            foreach (string subKeyName in key.GetSubKeyNames())
            {
                if (findings.Count >= maxRecords)
                {
                    break;
                }

                try
                {
                    ScanKey(root, $"{keyPath}\\{subKeyName}", findings, maxRecords, depth + 1);
                }
                catch
                {

                    // Skip unreadable subkeys.

                }
            }
        }
    }
}
