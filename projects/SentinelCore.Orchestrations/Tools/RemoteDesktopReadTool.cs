// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RemoteDesktopReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Remote Desktop settings.
/// </summary>
public sealed class RemoteDesktopReadTool : AITool
{

    private const string RdpTcpKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";
    private const string TerminalServerKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server";
    public override string Description { get; } = "Read-only tool for querying Remote Desktop settings.";
    public override string Name { get; } = "Remote_Desktop_Read";








    private static void ReadKeyValues(RegistryKey root, string keyPath, StringBuilder sb, string[] valueNames)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, false);
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








    [Description("Reads RDP listener port and security layer settings.")]
    public Task<ToolResult> rdp_read_listener_config()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(Registry.LocalMachine, RdpTcpKey, sb, ["PortNumber", "SecurityLayer", "MinEncryptionLevel", "UserAuthentication", "SSLCertificateSHA1Hash"]);

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"RDP listener config read failed: {ex.Message}"));
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

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"RDP settings read failed: {ex.Message}"));
        }
    }
}