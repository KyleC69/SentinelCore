// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ProxyReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying system proxy configuration from the registry and WinHTTP.
/// </summary>
public sealed class ProxyReadTool : AITool
{
    [Description("Reads the system proxy configuration from the Internet Settings registry key.")]
    public Task<ToolResult> proxy_read_system()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings", writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult("Internet Settings registry key not found."));
            }

            foreach (string valueName in key.GetValueNames())
                if (valueName.Contains("Proxy", StringComparison.OrdinalIgnoreCase) || valueName.Contains("AutoConfig", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"{valueName}={key.GetValue(valueName)}");
                }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"System proxy read failed: {ex.Message}"));
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

            using Process? process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return Task.FromResult(ToolResult.FailureResult("Failed to start netsh."));
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return Task.FromResult(ToolResult.FailureResult($"netsh failed: {stderr}"));
            }

            return Task.FromResult(ToolResult.SuccessResult(stdout));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"WinHTTP proxy read failed: {ex.Message}"));
        }
    }
}