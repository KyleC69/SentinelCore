// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PnpDeviceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Diagnostics;




namespace SentinelCore.Tools;





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

    public override string Description { get; } = "Read-only tool for querying Plug and Play devices through pnputil.exe.";
    public override string Name { get; } = "PnP_Device_Read";








    private static string EscapeArgument(string argument)
    {
        if (argument.Contains(' ') || argument.Contains('\t') || argument.Contains('"'))
            return $"\"{argument.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

        return argument;
    }








    private static ToolResult RunPnputil(List<string> arguments)
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

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            return ToolResult.Fail("Unable to start pnputil.exe.");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return ToolResult.Fail(string.IsNullOrWhiteSpace(error) ? $"pnputil exited with code {process.ExitCode}." : $"pnputil exited with code {process.ExitCode}: {error.Trim()}");
        }

        return ToolResult.Ok(output);
    }








    private static ToolResult? ValidateArguments(IEnumerable<string> arguments)
    {
        foreach (string arg in arguments)
            if (arg.StartsWith('/'))
            {
                if (DisallowedOptions.Contains(arg))
                {
                    return ToolResult.Fail($"PnP option '{arg}' is not allowed because it is destructive or state-changing.");
                }

                if (!AllowedOptions.Contains(arg))
                {
                    return ToolResult.Fail($"PnP option '{arg}' is not in the allowed whitelist.");
                }
            }

        return null;
    }








    [Description("Lists PnP devices using the pnputil /enum-devices command. Optional class and status filters are applied when provided.")]
    public Task<ToolResult> pnp_list_devices([Description("Optional device status filter. Use 'Problem' to filter problem devices or 'Connected' for connected devices.")] string? status = null, [Description("Optional device class name filter, e.g. Display, Net.")] string? className = null)
    {
        try
        {
            List<string> args = new() { "/enum-devices" };

            if (!string.IsNullOrWhiteSpace(className))
            {
                args.Add("/class");
                args.Add(className);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Problem", StringComparison.OrdinalIgnoreCase))
                    args.Add("/problem");
                else if (status.Equals("Connected", StringComparison.OrdinalIgnoreCase)) args.Add("/connected");
            }

            ToolResult? validation = ValidateArguments(args);
            if (validation is not null)
            {
                return Task.FromResult(validation);
            }

            return Task.FromResult(RunPnputil(args));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"PnP device listing failed: {ex.Message}"));
        }
    }








    [Description("Reads detailed properties for a specific PnP device using the pnputil /device-info command.")]
    public Task<ToolResult> pnp_read_device([Description("The PnP device instance ID to inspect, e.g. USB\\VID_1234&PID_5678\\0001.")] string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Task.FromResult(ToolResult.Fail("deviceId is required."));
            }

            List<string> args = new() { "/device-info", deviceId };
            ToolResult? validation = ValidateArguments(args);
            if (validation is not null)
            {
                return Task.FromResult(validation);
            }

            return Task.FromResult(RunPnputil(args));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"PnP device read failed: {ex.Message}"));
        }
    }
}
