// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         BrowserConfigReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for reading browser configuration stored in the Windows registry.
///     This covers common browser default settings and per-browser policy/proxy entries where available.
/// </summary>
[McpServerToolType]
public sealed class BrowserConfigReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_chrome_policies", ReadOnly = true, Destructive = false)]
    [Description("Reads Google Chrome policy entries from the registry if present.")]
    public async Task<ToolResult> BrowserReadChromePoliciesAsync()
    {
        try
        {
            List<object> results = new();
            ReadRegistryValues(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", results, "HKLM Chrome Policies");
            ReadRegistryValues(RegistryHive.CurrentUser, @"SOFTWARE\Policies\Google\Chrome", results, "HKCU Chrome Policies");
            return ToolResult.Ok(results, "BrowserConfigReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BrowserConfigReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_default_browser", ReadOnly = true, Destructive = false)]
    [Description("Reads the default browser ProgId from the registry.")]
    public async Task<ToolResult> BrowserReadDefaultAsync()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\\UserChoice", false);
            if (key is null)
            {
                return ToolResult.Fail("Operation failed", "BrowserConfigReadTool");
            }

            string progId = key.GetValue("ProgId")?.ToString() ?? string.Empty;
            return ToolResult.Ok($"DefaultBrowserProgId={progId}", "BrowserConfigReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BrowserConfigReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_ie_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads Internet Explorer / Edge proxy and security zone settings from the registry.")]
    public async Task<ToolResult> BrowserReadIeSettingsAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? proxyKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings", false);
            if (proxyKey is not null)
            {
                results.Add(new { Section = "Proxy", Name = "ProxyEnable", Value = Helpers.CleanValue(proxyKey.GetValue("ProxyEnable")?.ToString()) });
                results.Add(new { Section = "Proxy", Name = "ProxyServer", Value = Helpers.CleanValue(proxyKey.GetValue("ProxyServer")?.ToString()) });
                results.Add(new { Section = "Proxy", Name = "ProxyOverride", Value = Helpers.CleanValue(proxyKey.GetValue("ProxyOverride")?.ToString()) });
                results.Add(new { Section = "Proxy", Name = "AutoConfigURL", Value = Helpers.CleanValue(proxyKey.GetValue("AutoConfigURL")?.ToString()) });
            }

            using RegistryKey? zonesKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\\Zones", false);
            if (zonesKey is not null)
            {
                foreach (string zone in zonesKey.GetSubKeyNames())
                {
                    using RegistryKey? zoneSub = zonesKey.OpenSubKey(zone, false);
                    if (zoneSub is null)
                    {
                        continue;
                    }

                    results.Add(new { Section = "Zones", Name = zone, Value = Helpers.CleanValue(zoneSub.GetValue("CurrentLevel")?.ToString()) });
                }
            }

            return ToolResult.Ok(results, "BrowserConfigReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BrowserConfigReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    private static void ReadRegistryValues(RegistryHive hive, string path, List<object> results, string label)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(path, false);
        if (key is null)
        {
            return;
        }

        foreach (string valueName in key.GetValueNames())
        {
            results.Add(new { Section = label, Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
        }
    }
}
