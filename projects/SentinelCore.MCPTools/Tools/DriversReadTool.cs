// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         DriversReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.ServiceProcess;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating installed Windows drivers using the Service Control Manager (SCM) API.
///     Drivers are exposed as kernel services with service type SERVICE_KERNEL_DRIVER or SERVICE_FILE_SYSTEM_DRIVER.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class DriversReadTool
{

    /// <summary>
    ///     Lists installed kernel and file-system drivers via the Service Control Manager.
    /// </summary>
    /// <param name="typeFilter">Optional filter: kernel, filesystem, or all. Defaults to all.</param>
    /// <param name="count">Maximum number of drivers to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing the driver listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_drivers", ReadOnly = true, Destructive = false)]
    [Description("Lists installed kernel and file-system drivers via SCM. Use count to limit the number of results returned.")]
    public async Task<ToolResult> DriverListAsync([Description("Optional filter: kernel, filesystem, or all. Defaults to all.")] string? typeFilter = null, [Description("Maximum number of drivers to return. Defaults to 50.")] int count = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(count, "count");
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            string filter = typeFilter?.ToLowerInvariant() ?? "all";
            if (filter is not ("all" or "kernel" or "filesystem"))
            {
                return ToolResult.Fail($"Invalid typeFilter: {typeFilter}. Supported values: all, kernel, filesystem.", "DriversReadTool");
            }

            List<object> results = new();
            int resultCount = 0;
            foreach (ServiceController service in ServiceController.GetDevices())
            {
                if (resultCount >= count)
                {
                    break;
                }

                string kind = GetDriverKind(service.ServiceType);

                if (!filter.Equals("all", StringComparison.OrdinalIgnoreCase) && !kind.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(new
                {
                        Name = service.ServiceName,
                        DisplayName = Helpers.CleanValue(service.DisplayName),
                        Status = service.Status.ToString(),
                        StartType = service.StartType.ToString(),
                        Kind = kind
                });
                resultCount++;
            }

            return ToolResult.Ok(results, "DriversReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Driver listing");
        }
    }








    /// <summary>
    ///     Classifies a driver service based on its actual <see cref="ServiceType" /> flags
    ///     reported by the Service Control Manager, rather than name heuristics.
    /// </summary>
    /// <param name="serviceType">The service type flags from the SCM.</param>
    /// <returns>A human-readable driver kind: "kernel", "filesystem", or "recognizer".</returns>
    [SupportedOSPlatform("windows")]
    private static string GetDriverKind(ServiceType serviceType)
    {
        if (serviceType.HasFlag(ServiceType.FileSystemDriver))
        {
            return "filesystem";
        }

        if (serviceType.HasFlag(ServiceType.RecognizerDriver))
        {
            return "recognizer";
        }

        return "kernel";
    }
}
