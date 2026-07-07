// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         RemoteDesktopReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Remote Desktop settings.
/// </summary>
public sealed class RemoteDesktopReadTool : AITool
{
    private const string RdpTcpKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";
    private const string TerminalServerKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server";








    [Description("Reads RDP listener port and security layer settings.")]
    public Task<ToolResult> rdp_read_listener_config()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(Registry.LocalMachine, RdpTcpKey, sb, ["PortNumber", "SecurityLayer", "MinEncryptionLevel", "UserAuthentication", "SSLCertificateSHA1Hash"]);

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"RDP listener config read failed: {ex.Message}"));
        }
    }








    [Description("Reads Remote Desktop configuration from the registry.")]
    public Task<ToolResult> rdp_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(Registry.LocalMachine, TerminalServerKey, sb, ["fDenyTSConnections", "fSingleSessionPerUser", "UserAuthentication"]);
            ReadKeyValues(Registry.LocalMachine, RdpTcpKey, sb, ["PortNumber", "MinEncryptionLevel", "SecurityLayer", "UserAuthentication"]);

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"RDP settings read failed: {ex.Message}"));
        }
    }








    private static void ReadKeyValues(RegistryKey root, string keyPath, StringBuilder sb, string[] valueNames)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, writable: false);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"[{keyPath}]");
        foreach (string valueName in valueNames)
        {
            object? value = key.GetValue(valueName);
            if (value is not null)
            {
                sb.AppendLine($"  {valueName}={value}");
            }
        }
    }
}