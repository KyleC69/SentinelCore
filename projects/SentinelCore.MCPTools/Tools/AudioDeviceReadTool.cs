// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         AudioDeviceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying multimedia and audio device settings using pnputil and the
///     Windows registry instead of the Core Audio MMDevice COM API.
/// </summary>
[McpServerToolType]
public sealed class AudioDeviceReadTool
{



    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_audio_devices", ReadOnly = true, Destructive = false)]
    [Description("Lists active audio playback and recording devices using pnputil and the registry.")]
    public async Task<ToolResult> AudioListDevicesAsync()
    {
        try
        {
            // List connected audio endpoints (speakers, microphones, etc.)
            ToolResult endpointsResult = RunPnputil("/enum-devices /class AudioEndpoint /connected");
            if (!endpointsResult.Success)
            {
                return endpointsResult;
            }

            // List connected audio driver devices (sound cards)
            ToolResult mediaResult = RunPnputil("/enum-devices /class MEDIA /connected");
            if (!mediaResult.Success)
            {
                return mediaResult;
            }

            var result = new { AudioEndpoints = Helpers.CleanOutputLines(endpointsResult.Results?.ToString()), AudioDevices = Helpers.CleanOutputLines(mediaResult.Results?.ToString()) };

            return ToolResult.Ok(result, "AudioDeviceReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "AudioDeviceReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_default_audio_device", ReadOnly = true, Destructive = false)]
    [Description("Reads the default audio playback device from the registry.")]
    public async Task<ToolResult> AudioReadDefaultDeviceAsync()
    {
        try
        {
            // Read the default audio render endpoint from the MMDevices registry.
            // Active endpoints have DeviceState = 1 (DEVICE_STATE_ACTIVE).
            // The default device is identified by checking the MMDevices\Audio\Render subkeys.
            string renderKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
            using RegistryKey? renderKey = Registry.LocalMachine.OpenSubKey(renderKeyPath, false);
            if (renderKey is null)
            {
                return ToolResult.Fail("Operation failed", "AudioDeviceReadTool");
            }

            List<object> results = new();

            foreach (string endpointId in renderKey.GetSubKeyNames())
            {
                int? state = GetEndpointState(endpointId);
                if (state != 1) // Only show active devices
                {
                    continue;
                }

                string? friendlyName = GetEndpointFriendlyName(endpointId);
                results.Add(new { Id = endpointId, Name = Helpers.CleanValue(friendlyName) is { Length: > 0 } name ? name : "(unknown)", State = StateToString(state ?? 0) });
            }

            // Also check the Sound Mapper key for the default playback device
            string? defaultDevice = null;
            using (RegistryKey? soundMapper = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Multimedia\Sound Mapper", false))
            {
                if (soundMapper is not null)
                {
                    object? playback = soundMapper.GetValue("Playback");
                    defaultDevice = playback?.ToString();
                }
            }

            if (defaultDevice is not null)
            {
                results.Add(new { Id = "DefaultPlayback", Name = Helpers.CleanValue(defaultDevice), State = "Default" });
            }

            return ToolResult.Ok(results, "AudioDeviceReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "AudioDeviceReadTool");

        }
    }








    /// <summary>
    ///     Reads the friendly name for an audio endpoint from the MMDevices registry key.
    /// </summary>
    private static string? GetEndpointFriendlyName(string endpointId)
    {
        try
        {
            string keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{endpointId}\Properties";
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return null;
            }

            // The friendly name is stored in the {a45c254e-df1c-4efd-8020-67d146a850e0},2 value (DEVPKEY_Device_FriendlyName)
            object? val = key.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2");
            return val?.ToString();
        }
        catch
        {

            return null;

        }
    }








    /// <summary>
    ///     Reads the device state for an audio endpoint from the MMDevices registry key.
    ///     State values: 1 = Active, 2 = Disabled, 4 = Not present, 8 = Unplugged.
    /// </summary>
    private static int? GetEndpointState(string endpointId)
    {
        try
        {
            string keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{endpointId}";
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (key is null)
            {
                return null;
            }

            object? val = key.GetValue("DeviceState");
            return val is int state ? state : null;
        }
        catch
        {

            return null;

        }
    }








    /// <summary>
    ///     Runs pnputil with the specified arguments and returns the standard output,
    ///     or a failure result if the process cannot start or returns a non-zero exit code.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static ToolResult RunPnputil(string arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                    FileName = "pnputil",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "AudioDeviceReadTool");
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode != 0 ? ToolResult.Fail($"pnputil failed: {stderr}", "AudioDeviceReadTool") : ToolResult.Ok(Helpers.CleanOutputLines(stdout), "AudioDeviceReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "AudioDeviceReadTool");

        }
    }








    /// <summary>
    ///     Converts a device state value to a human-readable string.
    /// </summary>
    private static string StateToString(int state) => state switch
    {
            1 => "Active",
            2 => "Disabled",
            4 => "NotPresent",
            8 => "Unplugged",
            _ => $"Unknown({state})"
    };
}
