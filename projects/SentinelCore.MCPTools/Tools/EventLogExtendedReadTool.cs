// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         EventLogExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Event Log forwarding configuration
///     to detect log tampering or misconfigured forwarding.
/// </summary>
[McpServerToolType]
public sealed class EventLogExtendedReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_read_event_forwarding", ReadOnly = true, Destructive = false)]
    [Description("Reads Windows Event Forwarding (WEF) subscription configuration from the registry.")]
    public async Task<ToolResult> EventLogReadForwardingAsync()
    {
        try
        {
            StringBuilder sb = new();

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // WEF subscriptions
                using RegistryKey? wefKey = baseKey.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\EventLog\EventForwarding", false);
                if (wefKey is not null)
                {
                    sb.AppendLine("[Event Forwarding Policy]");
                    foreach (string valueName in wefKey.GetValueNames())
                    {
                        sb.AppendLine($"  {valueName}={wefKey.GetValue(valueName)}");
                    }

                    // Check subscription subkey
                    using RegistryKey? subKey = wefKey.OpenSubKey(@"SubscriptionManager", false);
                    if (subKey is not null)
                    {
                        sb.AppendLine("[Subscription Manager]");
                        foreach (string valueName in subKey.GetValueNames())
                        {
                            sb.AppendLine($"  {valueName}={subKey.GetValue(valueName)}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("[Event Forwarding Policy] Not configured.");
                }

                // Event Log service configuration
                using RegistryKey? evtKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\EventLog", false);
                if (evtKey is not null)
                {
                    sb.AppendLine("[EventLog Service]");
                    foreach (string subKeyName in evtKey.GetSubKeyNames())
                    {
                        using RegistryKey? subKey = evtKey.OpenSubKey(subKeyName, false);
                        if (subKey is not null)
                        {
                            object? start = subKey.GetValue("Start");
                            object? imagePath = subKey.GetValue("ImagePath");
                            if (start is not null || imagePath is not null)
                            {
                                sb.AppendLine($"  {subKeyName}: Start={start}, ImagePath={imagePath}");
                            }
                        }
                    }
                }
            }

            return ToolResult.Ok(sb.ToString(), "Event log forwarding read");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Event log forwarding read");
        }
    }
}
