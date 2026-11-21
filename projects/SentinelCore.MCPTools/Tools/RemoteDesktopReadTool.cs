// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RemoteDesktopReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Remote Desktop settings.
/// </summary>
[McpServerToolType]
public sealed class RemoteDesktopReadTool
{

    private const string RdpTcpKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";
    private const string TerminalServerKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server";








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_rdp_listener", ReadOnly = true, Destructive = false)]
    [Description("Reads RDP listener port and security layer settings.")]
    public async Task<ToolResult> RdpReadListenerConfigAsync()
    {
        try
        {
            List<object> results = new();
            ReadKeyValues(Registry.LocalMachine, RdpTcpKey, results, ["PortNumber", "SecurityLayer", "MinEncryptionLevel", "UserAuthentication", "SSLCertificateSHA1Hash"]);

            return ToolResult.Ok(results, "RemoteDesktopReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "RemoteDesktopReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_rdp_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads Remote Desktop configuration from the registry.")]
    public async Task<ToolResult> RdpReadSettingsAsync()
    {
        try
        {
            List<object> results = new();
            ReadKeyValues(Registry.LocalMachine, TerminalServerKey, results, ["fDenyTSConnections", "fSingleSessionPerUser", "UserAuthentication"]);
            ReadKeyValues(Registry.LocalMachine, RdpTcpKey, results, ["PortNumber", "MinEncryptionLevel", "SecurityLayer", "UserAuthentication"]);

            return ToolResult.Ok(results, "RemoteDesktopReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "RemoteDesktopReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    private static void ReadKeyValues(RegistryKey root, string keyPath, List<object> results, string[] valueNames)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        foreach (string valueName in valueNames)
        {
            object? value = key.GetValue(valueName);
            if (value is not null)
            {
                results.Add(new { Key = keyPath, Name = valueName, Value = Helpers.CleanValue(value.ToString()) });
            }
        }
    }
}
