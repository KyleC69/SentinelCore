// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         NetworkReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tools for querying network interfaces, TCP/IP configuration, and DNS.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class NetworkReadTool
{

    [McpServerTool(Name = "sc_networkaccess_list_adapters", ReadOnly = true, Destructive = false)]
    [Description("Lists network interfaces and their operational status.")]
    public async Task<ToolResult> NetworkListInterfacesAsync([Description("Maximum number of interfaces to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Take(maxRecords);
            var results = interfaces.Select(ni => new
            {
                    ni.Name,
                    ni.Description,
                    ni.OperationalStatus,
                    ni.Speed,
                    ni.NetworkInterfaceType,
                    ni.GetIPProperties().UnicastAddresses.Count
            });

            return ToolResult.Ok(results, "NetworkReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkReadTool");
        }
    }








    [McpServerTool(Name = "sc_networkaccess_list_tcp_connections", ReadOnly = true, Destructive = false)]
    [Description("Lists active TCP connections and their local/remote endpoints.")]
    public async Task<ToolResult> NetworkListTcpConnectionsAsync([Description("Maximum number of connections to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            var results = connections.Take(maxRecords).Select(c => new { LocalEndpoint = c.LocalEndPoint.ToString(), RemoteEndpoint = c.RemoteEndPoint.ToString(), c.State });

            return ToolResult.Ok(results, "NetworkReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkReadTool");
        }
    }








    [McpServerTool(Name = "sc_networkaccess_read_ip_config", ReadOnly = true, Destructive = false)]
    [Description("Reads IP configuration for a specific network interface.")]
    public async Task<ToolResult> NetworkReadIpConfigAsync([Description("The network interface name.")] string interfaceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                return ToolResult.Fail("interfaceName is required", "NetworkReadTool");
            }

            NetworkInterface? ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

            if (ni is null)
            {
                return ToolResult.Fail($"Network interface not found: {interfaceName}", "NetworkReadTool");
            }

            IPInterfaceProperties props = ni.GetIPProperties();
            StringBuilder sb = new();
            sb.AppendLine($"Name={ni.Name}");
            sb.AppendLine($"Description={ni.Description}");
            sb.AppendLine($"OperationalStatus={ni.OperationalStatus}");
            sb.AppendLine($"DnsSuffix={props.DnsSuffix}");
            sb.AppendLine("IPv4Addresses:");
            foreach (UnicastIPAddressInformation addr in props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
                sb.AppendLine($"  {addr.Address}/{addr.PrefixLength}");

            sb.AppendLine("IPv6Addresses:");
            foreach (UnicastIPAddressInformation addr in props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6))
                sb.AppendLine($"  {addr.Address}/{addr.PrefixLength}");

            sb.AppendLine("DnsServers:");
            foreach (IPAddress dns in props.DnsAddresses) sb.AppendLine($"  {dns}");

            sb.AppendLine("Gateways:");
            foreach (GatewayIPAddressInformation gw in props.GatewayAddresses) sb.AppendLine($"  {gw.Address}");

            return ToolResult.Ok(sb.ToString(), "NetworkReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkReadTool");
        }
    }








    [McpServerTool(Name = "sc_networkaccess_resolve_hostname", ReadOnly = true, Destructive = false)]
    [Description("Resolves a hostname to IP addresses using DNS.")]
    public async Task<ToolResult> NetworkResolveDnsAsync([Description("The hostname or domain to resolve.")] string hostName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return ToolResult.Fail("hostName is required", "NetworkReadTool");
            }

            IPHostEntry entries = Dns.GetHostEntry(hostName);
            StringBuilder sb = new();
            sb.AppendLine($"HostName={entries.HostName}");
            sb.AppendLine("Addresses:");
            foreach (IPAddress address in entries.AddressList) sb.AppendLine($"  {address}");

            sb.AppendLine("Aliases:");
            foreach (string alias in entries.Aliases) sb.AppendLine($"  {alias}");

            return ToolResult.Ok(sb.ToString(), "NetworkReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NetworkReadTool");
        }
    }
}
