// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ProxyReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying system proxy configuration from the registry
///     (user Internet Settings and machine WinHTTP settings).
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ProxyReadTool
{
    private const string WinHttpSettingsKey = @"SYSTEM\CurrentControlSet\Services\Http\Parameters\ProxySettings";








    /// <summary>
    ///     Reads the per-user system proxy configuration from the Internet Settings registry key.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing the user proxy settings.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_system_proxy", ReadOnly = true, Destructive = false)]
    [Description("Reads the system proxy configuration from the Internet Settings registry key.")]
    public async Task<ToolResult> ProxyReadSystemAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings", false);
            if (key is null)
            {
                return ToolResult.Fail("Proxy registry key not found.", "ProxyReadTool");
            }

            foreach (string valueName in key.GetValueNames())
            {
                if (valueName.Contains("Proxy", StringComparison.OrdinalIgnoreCase) || valueName.Contains("AutoConfig", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new { Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
                }
            }

            return ToolResult.Ok(results, "ProxyReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "System proxy read");
        }
    }








    /// <summary>
    ///     Reads the machine-wide WinHTTP proxy configuration from the registry
    ///     instead of shelling out to <c>netsh winhttp show proxy</c>.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing the WinHTTP proxy settings.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_read_winhttp_proxy", ReadOnly = true, Destructive = false)]
    [Description("Reads the machine-wide WinHTTP proxy configuration from the registry.")]
    public async Task<ToolResult> ProxyReadWinhttpAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(WinHttpSettingsKey, false);
            if (key is null)
            {
                return ToolResult.Ok(new { WinHttpSettings = "DirectAccess (no proxy configured)" }, "ProxyReadTool");
            }

            foreach (RegistryHelper.RegistryValueRecord v in RegistryHelper.ReadValues(key))
            {
                results.Add(new { v.Name, v.Value, v.Kind });
            }

            return ToolResult.Ok(results, "ProxyReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "WinHTTP proxy read");
        }
    }
}
