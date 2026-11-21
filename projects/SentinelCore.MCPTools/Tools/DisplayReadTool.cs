// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         DisplayReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying display configuration using CIM video classes,
///     replacing the user32.dll GetSystemMetrics P/Invoke per spec §6.2.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class DisplayReadTool
{

    /// <summary>
    ///     Lists active display monitors using the Win32_DesktopMonitor CIM class.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing typed monitor records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_monitors", ReadOnly = true, Destructive = false)]
    [Description("Lists active display monitors using the Win32_DesktopMonitor CIM class.")]
    public async Task<ToolResult> DisplayListMonitorsAsync()
    {
        try
        {
            List<MonitorRecord> results = await Task.Run(() =>
                    {
                        List<MonitorRecord> records = new();
                        using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT DeviceID, Name, ScreenWidth, ScreenHeight, PixelsPerXLogicalInch, PixelsPerYLogicalInch, Status FROM Win32_DesktopMonitor");
                        foreach (ManagementObject monitor in searcher.Get())
                        {
                            records.Add(new MonitorRecord(DeviceId: monitor["DeviceID"]?.ToString() ?? string.Empty, Name: monitor["Name"]?.ToString() ?? string.Empty, ScreenWidth: monitor["ScreenWidth"]?.ToString() ?? string.Empty, ScreenHeight: monitor["ScreenHeight"]?.ToString() ?? string.Empty, PixelsPerXLogicalInch: monitor["PixelsPerXLogicalInch"]?.ToString() ?? string.Empty, PixelsPerYLogicalInch: monitor["PixelsPerYLogicalInch"]?.ToString() ?? string.Empty, Status: monitor["Status"]?.ToString() ?? string.Empty));
                        }

                        return records;
                    })
                    .ConfigureAwait(false);

            return ToolResult.Ok(results, $"Enumerated {results.Count} monitor(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Display monitor listing");
        }
    }








    /// <summary>
    ///     Reads the display adapter (video controller) inventory via the Win32_VideoController CIM class.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing typed video controller records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_video_controllers", ReadOnly = true, Destructive = false)]
    [Description("Lists display adapters and their current video mode via the Win32_VideoController CIM class.")]
    public async Task<ToolResult> DisplayListVideoControllersAsync()
    {
        try
        {
            List<VideoControllerRecord> results = await Task.Run(() =>
                    {
                        List<VideoControllerRecord> records = new();
                        using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Name, AdapterCompatibility, AdapterRAM, VideoModeDescription, DriverVersion, Status FROM Win32_VideoController");
                        foreach (ManagementObject controller in searcher.Get())
                        {
                            records.Add(new VideoControllerRecord(Name: controller["Name"]?.ToString() ?? string.Empty, AdapterCompatibility: controller["AdapterCompatibility"]?.ToString() ?? string.Empty, AdapterRam: controller["AdapterRAM"]?.ToString() ?? string.Empty, VideoModeDescription: controller["VideoModeDescription"]?.ToString() ?? string.Empty, DriverVersion: controller["DriverVersion"]?.ToString() ?? string.Empty, Status: controller["Status"]?.ToString() ?? string.Empty));
                        }

                        return records;
                    })
                    .ConfigureAwait(false);

            return ToolResult.Ok(results, $"Enumerated {results.Count} video controller(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Video controller listing");
        }
    }








    /// <summary>
    ///     Reads the current video mode geometry via the Win32_VideoController CIM class,
    ///     replacing the user32.dll GetSystemMetrics P/Invoke.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing the virtual screen geometry.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_display_geometry", ReadOnly = true, Destructive = false)]
    [Description("Reads the current display geometry via the Win32_VideoController CIM class.")]
    public async Task<ToolResult> DisplayReadVirtualScreenAsync()
    {
        try
        {
            VirtualScreenRecord? record = await Task.Run(() =>
                    {
                        using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT CurrentHorizontalResolution, CurrentVerticalResolution, CurrentBitsPerPixel, VideoModeDescription FROM Win32_VideoController WHERE CurrentHorizontalResolution IS NOT NULL");
                        foreach (ManagementObject controller in searcher.Get())
                        {
                            return new VirtualScreenRecord(HorizontalResolution: controller["CurrentHorizontalResolution"] is ushort w ? w : 0, VerticalResolution: controller["CurrentVerticalResolution"] is ushort h ? h : 0, BitsPerPixel: controller["CurrentBitsPerPixel"] is ushort bpp ? bpp : 0, VideoMode: controller["VideoModeDescription"]?.ToString() ?? string.Empty);
                        }

                        return null;
                    })
                    .ConfigureAwait(false);

            if (record is null)
            {
                return ToolResult.Fail("Display record not found.", "DisplayReadTool");
            }

            return ToolResult.Ok(record, "Virtual screen geometry read.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Virtual screen read");
        }
    }








    /// <summary>
    ///     A single display monitor record.
    /// </summary>
    /// <param name="DeviceId">The PnP device identifier.</param>
    /// <param name="Name">The monitor name.</param>
    /// <param name="ScreenWidth">Horizontal resolution in pixels.</param>
    /// <param name="ScreenHeight">Vertical resolution in pixels.</param>
    /// <param name="PixelsPerXLogicalInch">Logical DPI on the X axis.</param>
    /// <param name="PixelsPerYLogicalInch">Logical DPI on the Y axis.</param>
    /// <param name="Status">The device status.</param>
    public sealed record MonitorRecord(string DeviceId, string Name, string ScreenWidth, string ScreenHeight, string PixelsPerXLogicalInch, string PixelsPerYLogicalInch, string Status);





    /// <summary>
    ///     A single video controller record.
    /// </summary>
    /// <param name="Name">The display adapter name.</param>
    /// <param name="AdapterCompatibility">The vendor.</param>
    /// <param name="AdapterRam">Adapter memory in bytes.</param>
    /// <param name="VideoModeDescription">Current video mode.</param>
    /// <param name="DriverVersion">The installed driver version.</param>
    /// <param name="Status">The device status.</param>
    public sealed record VideoControllerRecord(string Name, string AdapterCompatibility, string AdapterRam, string VideoModeDescription, string DriverVersion, string Status);





    /// <summary>
    ///     The virtual screen geometry payload.
    /// </summary>
    /// <param name="HorizontalResolution">Primary monitor horizontal resolution.</param>
    /// <param name="VerticalResolution">Primary monitor vertical resolution.</param>
    /// <param name="BitsPerPixel">Color depth in bits per pixel.</param>
    /// <param name="VideoMode">The current video mode description.</param>
    public sealed record VirtualScreenRecord(int HorizontalResolution, int VerticalResolution, int BitsPerPixel, string VideoMode);
}
