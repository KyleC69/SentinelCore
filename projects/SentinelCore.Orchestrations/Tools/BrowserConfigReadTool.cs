// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BrowserConfigReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for reading browser configuration stored in the Windows registry.
///     This covers common browser default settings and per-browser policy/proxy entries where available.
/// </summary>
public sealed class BrowserConfigReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for reading browser configuration from the Windows registry.";
    public override string Name { get; } = "Browser_Config_Read";








    private static void ReadRegistryValues(RegistryHive hive, string path, StringBuilder sb, string label)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(path, false);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"{label}:");
        foreach (string valueName in key.GetValueNames()) sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");
    }








    [Description("Reads Google Chrome policy entries from the registry if present.")]
    public Task<ToolResult> browser_read_chrome_policies()
    {
        try
        {
            StringBuilder sb = new();
            ReadRegistryValues(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", sb, "HKLM Chrome Policies");
            ReadRegistryValues(RegistryHive.CurrentUser, @"SOFTWARE\Policies\Google\Chrome", sb, "HKCU Chrome Policies");
            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Chrome policy read failed: {ex.Message}"));
        }
    }








    [Description("Reads the default browser ProgId from the registry.")]
    public Task<ToolResult> browser_read_default()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\\UserChoice", false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail("Default browser UserChoice key not found."));
            }

            string progId = key.GetValue("ProgId")?.ToString() ?? string.Empty;
            return Task.FromResult(ToolResult.Ok($"DefaultBrowserProgId={progId}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Default browser read failed: {ex.Message}"));
        }
    }








    [Description("Reads Internet Explorer / Edge proxy and security zone settings from the registry.")]
    public Task<ToolResult> browser_read_ie_settings()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? proxyKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings", false);
            if (proxyKey is not null)
            {
                sb.AppendLine($"ProxyEnable={proxyKey.GetValue("ProxyEnable")}");
                sb.AppendLine($"ProxyServer={proxyKey.GetValue("ProxyServer")}");
                sb.AppendLine($"ProxyOverride={proxyKey.GetValue("ProxyOverride")}");
                sb.AppendLine($"AutoConfigURL={proxyKey.GetValue("AutoConfigURL")}");
            }

            using RegistryKey? zonesKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\\Zones", false);
            if (zonesKey is not null)
            {
                sb.AppendLine("Zones:");
                foreach (string zone in zonesKey.GetSubKeyNames())
                {
                    using RegistryKey? zoneSub = zonesKey.OpenSubKey(zone, false);
                    if (zoneSub is null)
                    {
                        continue;
                    }

                    sb.AppendLine($"  Zone={zone}, CurrentLevel={zoneSub.GetValue("CurrentLevel")}");
                }
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"IE/Edge browser settings read failed: {ex.Message}"));
        }
    }
}