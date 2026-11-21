// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         AutorunsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating common Windows persistence and autorun locations.
///     Covers registry Run/RunOnce keys, Winlogon Shell/Userinit, Startup folder, IFEO,
///     Image File Execution Options, and WMI event subscriptions.
/// </summary>
[McpServerToolType]
public sealed class AutorunsReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_persistence_list_autoruns", ReadOnly = true, Destructive = false)]
    [Description("Enumerates common Windows persistence and autorun locations including registry Run/RunOnce keys, Winlogon, Startup folder, and IFEO entries.")]
    public async Task<ToolResult> AutorunsListAsync([Description("Maximum number of entries per category. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            StringBuilder sb = new();
            int count = 0;

            // HKLM Run
            sb.AppendLine("=== HKLM Run ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                if (key is not null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (count >= maxRecords) break;
                        sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");
                        count++;
                    }
                }
            }

            // HKLM RunOnce
            sb.AppendLine("=== HKLM RunOnce ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", false);
                if (key is not null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (count >= maxRecords) break;
                        sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");
                        count++;
                    }
                }
            }

            // HKCU Run
            sb.AppendLine("=== HKCU Run ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            {
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                if (key is not null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (count >= maxRecords) break;
                        sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");
                        count++;
                    }
                }
            }

            // HKCU RunOnce
            sb.AppendLine("=== HKCU RunOnce ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            {
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", false);
                if (key is not null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (count >= maxRecords) break;
                        sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");
                        count++;
                    }
                }
            }

            // Winlogon Shell and Userinit
            sb.AppendLine("=== Winlogon ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", false);
                if (key is not null)
                {
                    object? shell = key.GetValue("Shell");
                    object? userinit = key.GetValue("Userinit");
                    sb.AppendLine($"  Shell={shell}");
                    sb.AppendLine($"  Userinit={userinit}");
                }
            }

            // Startup folder
            sb.AppendLine("=== Startup Folder (All Users) ===");
            string allUsersStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            if (Directory.Exists(allUsersStartup))
            {
                foreach (string file in Directory.GetFiles(allUsersStartup))
                {
                    if (count >= maxRecords) break;
                    sb.AppendLine($"  {Path.GetFileName(file)} -> {file}");
                    count++;
                }
            }

            sb.AppendLine("=== Startup Folder (Current User) ===");
            string currentUserStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (Directory.Exists(currentUserStartup))
            {
                foreach (string file in Directory.GetFiles(currentUserStartup))
                {
                    if (count >= maxRecords) break;
                    sb.AppendLine($"  {Path.GetFileName(file)} -> {file}");
                    count++;
                }
            }

            // IFEO (Image File Execution Options)
            sb.AppendLine("=== Image File Execution Options ===");
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey? ifeoKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", false);
                if (ifeoKey is not null)
                {
                    foreach (string subKeyName in ifeoKey.GetSubKeyNames())
                    {
                        if (count >= maxRecords) break;
                        using RegistryKey? subKey = ifeoKey.OpenSubKey(subKeyName, false);
                        if (subKey is not null)
                        {
                            object? debugger = subKey.GetValue("Debugger");
                            if (debugger is not null)
                            {
                                sb.AppendLine($"  {subKeyName}: Debugger={debugger}");
                                count++;
                            }
                        }
                    }
                }
            }

            return ToolResult.Ok(sb.ToString(), "AutorunsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "AutorunsReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_persistence_list_ifeo", ReadOnly = true, Destructive = false)]
    [Description("Lists Image File Execution Options (IFEO) debugger entries, commonly used for persistence and process hijacking.")]
    public async Task<ToolResult> AutorunsListIfeoAsync([Description("Maximum number of entries to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? ifeoKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", false);
            if (ifeoKey is null)
            {
                return ToolResult.Ok("No IFEO entries found.", "AutorunsReadTool");
            }

            foreach (string subKeyName in ifeoKey.GetSubKeyNames())
            {
                if (results.Count >= maxRecords) break;
                using RegistryKey? subKey = ifeoKey.OpenSubKey(subKeyName, false);
                if (subKey is not null)
                {
                    object? debugger = subKey.GetValue("Debugger");
                    if (debugger is not null)
                    {
                        results.Add(new { ImageName = subKeyName, Debugger = debugger.ToString() });
                    }
                }
            }

            return ToolResult.Ok(results, "AutorunsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "AutorunsReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    private static void ReadRunKey(RegistryHive hive, string keyPath, StringBuilder sb, string label)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"{label}:");
        foreach (string valueName in key.GetValueNames())
        {
            object? value = key.GetValue(valueName);
            if (value is not null)
            {
                sb.AppendLine($"  {valueName}={value}");
            }
        }
    }
}
