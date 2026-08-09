// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NetworkReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tools for querying network interfaces, TCP/IP configuration, and DNS.
/// </summary>
public sealed class NetworkReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying network interfaces, TCP/IP configuration, and DNS.";
    public override string Name { get; } = "Network_Read";








    [Description("Lists network interfaces and their operational status.")]
    public Task<ToolResult> network_list_interfaces()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var results = interfaces.Select(ni => new
            {
                    ni.Name,
                    ni.Description,
                    ni.OperationalStatus,
                    ni.Speed,
                    ni.NetworkInterfaceType,
                    ni.GetIPProperties().UnicastAddresses.Count
            });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Network interface listing failed: {ex.Message}"));
        }
    }








    [Description("Lists active TCP connections and their local/remote endpoints.")]
    public Task<ToolResult> network_list_tcp_connections()
    {
        try
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            var results = connections.Select(c => new { LocalEndpoint = c.LocalEndPoint.ToString(), RemoteEndpoint = c.RemoteEndPoint.ToString(), c.State });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"TCP connection listing failed: {ex.Message}"));
        }
    }








    [Description("Reads IP configuration for a specific network interface.")]
    public Task<ToolResult> network_read_ip_config([Description("The network interface name.")] string interfaceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                return Task.FromResult(ToolResult.Fail("interfaceName is required."));
            }

            NetworkInterface? ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

            if (ni is null)
            {
                return Task.FromResult(ToolResult.Fail($"Network interface not found: {interfaceName}"));
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

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"IP config read failed: {ex.Message}"));
        }
    }








    [Description("Resolves a hostname to IP addresses using DNS.")]
    public Task<ToolResult> network_resolve_dns([Description("The hostname or domain to resolve.")] string hostName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return Task.FromResult(ToolResult.Fail("hostName is required."));
            }

            IPHostEntry entries = Dns.GetHostEntry(hostName);
            StringBuilder sb = new();
            sb.AppendLine($"HostName={entries.HostName}");
            sb.AppendLine("Addresses:");
            foreach (IPAddress address in entries.AddressList) sb.AppendLine($"  {address}");

            sb.AppendLine("Aliases:");
            foreach (string alias in entries.Aliases) sb.AppendLine($"  {alias}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"DNS resolution failed: {ex.Message}"));
        }
    }
}