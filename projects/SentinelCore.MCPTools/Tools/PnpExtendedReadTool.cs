// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PnpExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying USB device connection history from the registry.
///     Reads USBSTOR and setupapi.dev.log entries for removable media forensics.
/// </summary>
[McpServerToolType]
public sealed class PnpExtendedReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_usb_history", ReadOnly = true, Destructive = false)]
    [Description("Lists USB device connection history from the registry (USBSTOR entries) for removable media forensics.")]
    public async Task<ToolResult> PnpListUsbHistoryAsync([Description("Maximum number of devices to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();

            // Read USBSTOR entries from registry
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey? usbstorKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR", false);
                if (usbstorKey is not null)
                {
                    foreach (string deviceClass in usbstorKey.GetSubKeyNames())
                    {
                        if (results.Count >= maxRecords) break;

                        using RegistryKey? deviceClassKey = usbstorKey.OpenSubKey(deviceClass, false);
                        if (deviceClassKey is null) continue;

                        foreach (string deviceId in deviceClassKey.GetSubKeyNames())
                        {
                            if (results.Count >= maxRecords) break;

                            using RegistryKey? deviceKey = deviceClassKey.OpenSubKey(deviceId, false);
                            if (deviceKey is null) continue;

                            object? friendlyName = deviceKey.GetValue("FriendlyName");
                            object? classGuid = deviceKey.GetValue("ClassGUID");
                            object? driver = deviceKey.GetValue("Driver");
                            object? service = deviceKey.GetValue("Service");

                            results.Add(new
                            {
                                    DeviceClass = deviceClass,
                                    DeviceId = deviceId,
                                    FriendlyName = friendlyName?.ToString() ?? "",
                                    ClassGuid = classGuid?.ToString() ?? "",
                                    Driver = driver?.ToString() ?? "",
                                    Service = service?.ToString() ?? ""
                            });
                        }
                    }
                }

                // Also read USB entries
                using RegistryKey? usbKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB", false);
                if (usbKey is not null)
                {
                    foreach (string vendorProduct in usbKey.GetSubKeyNames())
                    {
                        if (results.Count >= maxRecords) break;

                        using RegistryKey? vpKey = usbKey.OpenSubKey(vendorProduct, false);
                        if (vpKey is null) continue;

                        foreach (string instanceId in vpKey.GetSubKeyNames())
                        {
                            if (results.Count >= maxRecords) break;

                            using RegistryKey? instKey = vpKey.OpenSubKey(instanceId, false);
                            if (instKey is null) continue;

                            object? friendlyName = instKey.GetValue("FriendlyName");
                            object? classGuid = instKey.GetValue("ClassGUID");
                            object? service = instKey.GetValue("Service");

                            results.Add(new
                            {
                                    DeviceClass = "USB",
                                    DeviceId = $"{vendorProduct}\\{instanceId}",
                                    FriendlyName = friendlyName?.ToString() ?? "",
                                    ClassGuid = classGuid?.ToString() ?? "",
                                    Service = service?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return ToolResult.Ok(results, "PnpExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PnpExtendedReadTool");
        }
    }
}
