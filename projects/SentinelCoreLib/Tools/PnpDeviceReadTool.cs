// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PnpDeviceReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Diagnostics;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Plug and Play devices through the pnputil.exe command.
///     Only non-destructive pnputil options are permitted; any disallowed option is rejected.
/// </summary>
public sealed class PnpDeviceReadTool : AITool
{
    private static readonly HashSet<string> AllowedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
            "/enum-devices",
            "/device-info",
            "/properties",
            "/status",
            "/class",
            "/connected",
            "/problem",
            "/ids",
            "/instanceids",
            "/format:csv",
            "/format:table",
            "/?"
    };

    private static readonly HashSet<string> DisallowedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
            "/add-driver",
            "/delete-driver",
            "/install",
            "/delete",
            "/disable",
            "/enable",
            "/remove",
            "/export-driver",
            "/import-driver",
            "/scan-devices",
            "/update-driver",
            "/export"
    };








    private static string EscapeArgument(string argument)
    {
        if (argument.Contains(' ') || argument.Contains('\t') || argument.Contains('"'))
        {
            return $"\"{argument.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        return argument;
    }








    [Description("Lists PnP devices using the pnputil /enum-devices command. Optional class and status filters are applied when provided.")]
    public Task<ToolResult> pnp_list_devices([Description("Optional device status filter. Use 'Problem' to filter problem devices or 'Connected' for connected devices.")] string? status = null, [Description("Optional device class name filter, e.g. Display, Net.")] string? className = null)
    {
        try
        {
            var args = new List<string> { "/enum-devices" };

            if (!string.IsNullOrWhiteSpace(className))
            {
                args.Add("/class");
                args.Add(className);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Problem", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add("/problem");
                }
                else if (status.Equals("Connected", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add("/connected");
                }
            }

            ToolResult? validation = ValidateArguments(args);
            if (validation is not null)
            {
                return Task.FromResult(validation);
            }

            string output = RunPnputil(args);
            return Task.FromResult(ToolResult.SuccessResult(output));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"PnP device listing failed: {ex.Message}"));
        }
    }








    [Description("Reads detailed properties for a specific PnP device using the pnputil /device-info command.")]
    public Task<ToolResult> pnp_read_device([Description("The PnP device instance ID to inspect, e.g. USB\\VID_1234&PID_5678\\0001.")] string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Task.FromResult(ToolResult.FailureResult("deviceId is required."));
            }

            List<string> args = new() { "/device-info", deviceId };
            ToolResult? validation = ValidateArguments(args);
            if (validation is not null)
            {
                return Task.FromResult(validation);
            }

            string output = RunPnputil(args);
            return Task.FromResult(ToolResult.SuccessResult(output));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"PnP device read failed: {ex.Message}"));
        }
    }








    private static string RunPnputil(List<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
                FileName = "pnputil.exe",
                Arguments = string.Join(" ", arguments.Select(EscapeArgument)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start pnputil.exe.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"pnputil exited with code {process.ExitCode}." : $"pnputil exited with code {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }








    private static ToolResult? ValidateArguments(IEnumerable<string> arguments)
    {
        foreach (string arg in arguments)
            if (arg.StartsWith('/'))
            {
                if (DisallowedOptions.Contains(arg))
                {
                    return ToolResult.FailureResult($"PnP option '{arg}' is not allowed because it is destructive or state-changing.");
                }

                if (!AllowedOptions.Contains(arg))
                {
                    return ToolResult.FailureResult($"PnP option '{arg}' is not in the allowed whitelist.");
                }
            }

        return null;
    }
}