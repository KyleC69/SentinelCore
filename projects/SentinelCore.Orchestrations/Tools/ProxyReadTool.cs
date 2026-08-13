// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ProxyReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying system proxy configuration from the registry and WinHTTP.
/// </summary>
public sealed class ProxyReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying system proxy configuration from the registry and WinHTTP.";
    public override string Name { get; } = "Proxy_Read";








    [Description("Reads the system proxy configuration from the Internet Settings registry key.")]
    public Task<ToolResult> proxy_read_system()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings", false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail("Internet Settings registry key not found."));
            }

            foreach (string valueName in key.GetValueNames())
                if (valueName.Contains("Proxy", StringComparison.OrdinalIgnoreCase) || valueName.Contains("AutoConfig", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"{valueName}={key.GetValue(valueName)}");
                }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"System proxy read failed: {ex.Message}"));
        }
    }








    [Description("Reads the WinHTTP proxy configuration using the netsh native command (read-only).")]
    public Task<ToolResult> proxy_read_winhttp()
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                    FileName = "netsh",
                    Arguments = "winhttp show proxy",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return Task.FromResult(ToolResult.Fail("Failed to start netsh."));
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return Task.FromResult(ToolResult.Fail($"netsh failed: {stderr}"));
            }

            return Task.FromResult(ToolResult.Ok(stdout));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"WinHTTP proxy read failed: {ex.Message}"));
        }
    }
}