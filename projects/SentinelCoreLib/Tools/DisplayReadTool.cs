// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         DisplayReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying display configuration using Win32 display APIs and CIM video classes.
/// </summary>
public sealed class DisplayReadTool : AITool
{
    [Description("Lists active display monitors using the Win32 display CIM classes.")]
    public Task<ToolResult> display_list_monitors()
    {
        try
        {
            var results = new List<object>();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT DeviceID, Name, ScreenWidth, ScreenHeight, PixelsPerXLogicalInch, PixelsPerYLogicalInch FROM Win32_DesktopMonitor");
            foreach (ManagementObject monitor in searcher.Get())
                results.Add(new
                {
                        DeviceID = monitor["DeviceID"]?.ToString(),
                        Name = monitor["Name"]?.ToString(),
                        ScreenWidth = monitor["ScreenWidth"]?.ToString(),
                        ScreenHeight = monitor["ScreenHeight"]?.ToString(),
                        PixelsPerXLogicalInch = monitor["PixelsPerXLogicalInch"]?.ToString(),
                        PixelsPerYLogicalInch = monitor["PixelsPerYLogicalInch"]?.ToString()
                });

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Display monitor listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the virtual screen geometry using Win32 API (GetSystemMetrics).")]
    public Task<ToolResult> display_read_virtual_screen()
    {
        try
        {
            int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int cx = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            int cy = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
            int monitors = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMONITORS);

            return Task.FromResult(ToolResult.SuccessResult($"VirtualScreen=({x},{y},{cx},{cy}), Monitors={monitors}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Virtual screen read failed: {ex.Message}"));
        }
    }








    private static class NativeMethods
    {
        public const int SM_CMONITORS = 80;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;








        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);
    }
}