// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         DefenderExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Microsoft Defender SmartScreen reputation-based protection status.
/// </summary>
[McpServerToolType]
public sealed class DefenderExtendedReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_smartscreen", ReadOnly = true, Destructive = false)]
    [Description("Reads Windows Defender SmartScreen and reputation-based protection settings from the registry.")]
    public async Task<ToolResult> DefenderReadSmartScreenAsync()
    {
        try
        {
            List<object> results = new();

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // SmartScreen settings
                using RegistryKey? smartScreenKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SmartScreenEnabled", false);
                if (smartScreenKey is not null)
                {
                    results.Add(new { Section = "SmartScreen Enabled", Name = "(Default)", Value = Helpers.CleanValue(smartScreenKey.GetValue(null)?.ToString()) });
                }

                // SmartScreen policy
                using RegistryKey? policyKey = baseKey.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", false);
                if (policyKey is not null)
                {
                    results.Add(new { Section = "SmartScreen Policy", Name = "EnableSmartScreen", Value = Helpers.CleanValue(policyKey.GetValue("EnableSmartScreen")?.ToString()) });
                    results.Add(new { Section = "SmartScreen Policy", Name = "ShellSmartScreenEnabled", Value = Helpers.CleanValue(policyKey.GetValue("ShellSmartScreenEnabled")?.ToString()) });
                }

                // Windows Defender Smart App Control
                using RegistryKey? sacKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy", false);
                if (sacKey is not null)
                {
                    foreach (string valueName in sacKey.GetValueNames())
                    {
                        results.Add(new { Section = "Smart App Control / CI Policy", Name = valueName, Value = Helpers.CleanValue(sacKey.GetValue(valueName)?.ToString()) });
                    }
                }

                // Microsoft Defender Application Guard
                using RegistryKey? appGuardKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Application Guard", false);
                if (appGuardKey is not null)
                {
                    foreach (string valueName in appGuardKey.GetValueNames())
                    {
                        results.Add(new { Section = "Application Guard", Name = valueName, Value = Helpers.CleanValue(appGuardKey.GetValue(valueName)?.ToString()) });
                    }
                }

                // Windows Defender Attack Surface Reduction
                using RegistryKey? asrKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR", false);
                if (asrKey is not null)
                {
                    foreach (string valueName in asrKey.GetValueNames())
                    {
                        results.Add(new { Section = "Attack Surface Reduction", Name = valueName, Value = Helpers.CleanValue(asrKey.GetValue(valueName)?.ToString()) });
                    }
                }
            }

            // HKCU SmartScreen
            using (RegistryKey hkcu = Registry.CurrentUser)
            {
                using RegistryKey? smKey = hkcu.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost\EnableSmartScreen", false);
                if (smKey is not null)
                {
                    results.Add(new { Section = "SmartScreen HKCU", Name = "(Default)", Value = Helpers.CleanValue(smKey.GetValue(null)?.ToString()) });
                }
            }

            return ToolResult.Ok(results, "DefenderExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "DefenderExtendedReadTool");
        }
    }
}
