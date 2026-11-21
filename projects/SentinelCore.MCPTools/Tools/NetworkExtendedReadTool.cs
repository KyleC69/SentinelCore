// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         NetworkExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tools for querying extended network state: listening ports, DNS cache, ARP table,
///     routing table, and network shares.
/// </summary>
[McpServerToolType]
public sealed class NetworkExtendedReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_list_listening_ports", ReadOnly = true, Destructive = false)]
    [Description("Lists TCP and UDP listening ports with process association where available.")]
    public async Task<ToolResult> NetworkListListeningPortsAsync([Description("Maximum number of ports to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            List<object> results = new();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            // TCP listeners
            foreach (IPEndPoint endpoint in properties.GetActiveTcpListeners())
            {
                if (results.Count >= maxRecords) break;
                results.Add(new { Protocol = "TCP", endpoint.Address, endpoint.Port, State = "Listen" });
            }

            // UDP listeners
            foreach (IPEndPoint endpoint in properties.GetActiveUdpListeners())
            {
                if (results.Count >= maxRecords) break;
                results.Add(new { Protocol = "UDP", endpoint.Address, endpoint.Port, State = "Listen" });
            }

            return ToolResult.Ok(results, "NetworkExtendedReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "Listening port listing");

        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_list_shares", ReadOnly = true, Destructive = false)]
    [Description("Lists network shares on the local machine.")]
    public async Task<ToolResult> NetworkListSharesAsync([Description("Maximum number of shares to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            ProcessStartInfo psi = new()
            {
                    FileName = "net",
                    Arguments = "share",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "NetworkExtendedReadTool");
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (results.Count >= maxRecords) break;
                string trimmed = line.Trim();
                // Skip header and separator lines
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("-") || trimmed.StartsWith("Share", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    results.Add(new { ShareName = parts[0], Resource = parts.Length >= 2 ? parts[1] : "", Remark = parts.Length >= 3 ? string.Join(" ", parts[2..]) : "" });
                }
            }

            return ToolResult.Ok(results, "NetworkExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkExtendedReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_arp_table", ReadOnly = true, Destructive = false)]
    [Description("Reads the ARP cache table mapping IP addresses to physical addresses.")]
    public async Task<ToolResult> NetworkReadArpTableAsync([Description("Maximum number of entries to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            ProcessStartInfo psi = new()
            {
                    FileName = "arp",
                    Arguments = "-a",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "NetworkExtendedReadTool");
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse arp -a output
            string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (results.Count >= maxRecords) break;
                string trimmed = line.Trim();
                // Lines with IP/MAC look like: 192.168.1.1   00-11-22-33-44-55   dynamic
                if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
                {
                    string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        results.Add(new { IPAddress = parts[0], PhysicalAddress = parts[1], Type = parts.Length >= 3 ? parts[2] : "" });
                    }
                }
            }

            return ToolResult.Ok(results, "NetworkExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkExtendedReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_dns_cache", ReadOnly = true, Destructive = false)]
    [Description("Reads the local DNS resolver cache entries.")]
    public async Task<ToolResult> NetworkReadDnsCacheAsync([Description("Maximum number of entries to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            // The managed IPGlobalProperties API does not expose the DNS cache.
            // Use the ipconfig /displaydns command to retrieve it.
            StringBuilder sb = new();
            ProcessStartInfo psi = new()
            {
                    FileName = "ipconfig",
                    Arguments = "/displaydns",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "NetworkExtendedReadTool");
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse the output to extract DNS cache entries
            List<object> entries = new();
            string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string? currentName = null;
            string? currentType = null;
            string? currentData = null;
            int count = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Record Name", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentName is not null && count < maxRecords)
                    {
                        entries.Add(new { RecordName = currentName, RecordType = currentType ?? "", Data = currentData ?? "" });
                        count++;
                    }

                    currentName = line.Substring(line.IndexOf('.') + 1).Trim();
                    currentType = null;
                    currentData = null;
                }
                else if (line.StartsWith("Record Type", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line.Substring(line.IndexOf('.') + 1).Trim();
                }
                else if (line.StartsWith("A (Host) Record", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Data", StringComparison.OrdinalIgnoreCase))
                {
                    currentData = line.Substring(line.IndexOf('.') + 1).Trim();
                }
            }

            // Add last entry
            if (currentName is not null && count < maxRecords)
            {
                entries.Add(new { RecordName = currentName, RecordType = currentType ?? "", Data = currentData ?? "" });
            }

            return ToolResult.Ok(entries, "NetworkExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkExtendedReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_routing_table", ReadOnly = true, Destructive = false)]
    [Description("Reads the IPv4 routing table.")]
    public async Task<ToolResult> NetworkReadRoutingTableAsync([Description("Maximum number of routes to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            ProcessStartInfo psi = new()
            {
                    FileName = "route",
                    Arguments = "print -4",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "NetworkExtendedReadTool");
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse route print output for IPv4 active routes
            string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool inRoutes = false;
            foreach (string line in lines)
            {
                if (results.Count >= maxRecords) break;
                string trimmed = line.Trim();
                if (trimmed.StartsWith("Network Destination", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("====", StringComparison.OrdinalIgnoreCase))
                {
                    inRoutes = true;
                    continue;
                }

                if (inRoutes && trimmed.Length > 0 && char.IsDigit(trimmed[0]))
                {
                    string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        results.Add(new
                        {
                                NetworkDestination = parts[0],
                                Netmask = parts[1],
                                Gateway = parts[2],
                                Interface = parts[3],
                                Metric = parts.Length >= 5 ? parts[4] : ""
                        });
                    }
                }
            }

            return ToolResult.Ok(results, "NetworkExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkExtendedReadTool");
        }
    }
}
