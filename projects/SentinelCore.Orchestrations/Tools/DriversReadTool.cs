// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DriversReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.ServiceProcess;
using System.Text;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating installed Windows drivers using the Service Control Manager (SCM) API.
///     Drivers are exposed as kernel services with service type SERVICE_KERNEL_DRIVER or SERVICE_FILE_SYSTEM_DRIVER.
/// </summary>
public sealed class DriversReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for enumerating installed Windows drivers via the Service Control Manager API.";
    public override string Name { get; } = "Drivers_Read";








    private static string GetDriverKind(string serviceName)
    {
        using ServiceController service = new(serviceName);
        // ServiceController does not expose ServiceType directly, but device services are by definition kernel/file-system drivers.
        // A conservative classification: file-system drivers commonly include 'fs' naming; otherwise report kernel.
        if (serviceName.Contains("fs", StringComparison.OrdinalIgnoreCase) || serviceName.Contains("filter", StringComparison.OrdinalIgnoreCase))
        {
            return "filesystem";
        }

        return "kernel";
    }








    [Description("Lists installed kernel and file-system drivers via SCM.")]
    public Task<ToolResult> driver_list([Description("Optional filter: kernel, filesystem, or all. Defaults to all.")] string? typeFilter = null)
    {
        try
        {
            string filter = typeFilter != null ? typeFilter.ToLowerInvariant() : "all";
            StringBuilder sb = new();
            var services = ServiceController.GetDevices();
            foreach (ServiceController service in services)
            {
                string kind;
                try
                {
                    kind = GetDriverKind(service.ServiceName);
                }
                catch
                {
                    kind = "unknown";
                }

                if (!filter.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    if (filter == "kernel" && !kind.Contains("kernel", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (filter == "filesystem" && !kind.Contains("filesystem", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                sb.AppendLine($"Name={service.ServiceName}, DisplayName={service.DisplayName}, Status={service.Status}, StartType={service.StartType}, Kind={kind}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Driver listing failed: {ex.Message}"));
        }
    }
}