// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         NetworkDnsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying DNS server settings from the registry
///     to detect DNS hijacking or misconfiguration.
/// </summary>
[McpServerToolType]
public sealed class NetworkDnsReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_dns_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads DNS server configuration settings from the registry for DNS hijacking detection.")]
    public async Task<ToolResult> NetworkReadDnsSettingsAsync()
    {
        try
        {
            List<object> results = new();

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // DNS Client settings
                using RegistryKey? dnsKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", false);
                if (dnsKey is not null)
                {
                    foreach (string valueName in dnsKey.GetValueNames())
                    {
                        results.Add(new { Section = "DNS Client Parameters", Name = valueName, Value = Helpers.CleanValue(dnsKey.GetValue(valueName)?.ToString()) });
                    }
                }

                // DNS Server settings (if this machine is a DNS server)
                using RegistryKey? dnsServerKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\DNS Server", false);
                if (dnsServerKey is not null)
                {
                    foreach (string valueName in dnsServerKey.GetValueNames())
                    {
                        results.Add(new { Section = "DNS Server", Name = valueName, Value = Helpers.CleanValue(dnsServerKey.GetValue(valueName)?.ToString()) });
                    }
                }

                // TCP/IP DNS settings per interface
                using RegistryKey? tcpIpKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", false);
                if (tcpIpKey is not null)
                {
                    foreach (string valueName in tcpIpKey.GetValueNames())
                    {
                        results.Add(new { Section = "TCP/IP DNS Parameters", Name = valueName, Value = Helpers.CleanValue(tcpIpKey.GetValue(valueName)?.ToString()) });
                    }
                }

                // DNS over HTTPS
                using RegistryKey? dohKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers", false);
                if (dohKey is not null)
                {
                    foreach (string valueName in dohKey.GetValueNames())
                    {
                        results.Add(new { Section = "DNS over HTTPS Well-Known Servers", Name = valueName, Value = Helpers.CleanValue(dohKey.GetValue(valueName)?.ToString()) });
                    }
                }
            }

            return ToolResult.Ok(results, "NetworkDnsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkDnsReadTool");
        }
    }
}
