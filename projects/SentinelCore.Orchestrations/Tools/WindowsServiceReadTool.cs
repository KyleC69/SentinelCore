// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WindowsServiceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.ServiceProcess;
using System.Text;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows services.
/// </summary>
public sealed class WindowsServiceReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying Windows services.";
    public override string Name { get; } = "Windows_Service_Read";








    [Description("Lists installed Windows services and their current status.")]
    public Task<ToolResult> service_list([Description("Optional service name filter (partial match).")] string? nameFilter = null)
    {
        try
        {
            var services = ServiceController.GetServices();
            StringBuilder sb = new();
            foreach (ServiceController service in services)
            {
                if (!string.IsNullOrWhiteSpace(nameFilter) && service.ServiceName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                sb.AppendLine($"Name={service.ServiceName}, DisplayName={service.DisplayName}, Status={service.Status}, StartType={service.StartType}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Service listing failed: {ex.Message}"));
        }
    }








    [Description("Reads detailed information about a specific Windows service.")]
    public Task<ToolResult> service_read([Description("The service name (not display name) to inspect.")] string serviceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return Task.FromResult(ToolResult.Fail("serviceName is required."));
            }

            using ServiceController service = new(serviceName);
            service.Refresh();

            StringBuilder sb = new();
            sb.AppendLine($"Name={service.ServiceName}");
            sb.AppendLine($"DisplayName={service.DisplayName}");
            sb.AppendLine($"Status={service.Status}");
            sb.AppendLine($"StartType={service.StartType}");
            sb.AppendLine($"CanPauseAndContinue={service.CanPauseAndContinue}");
            sb.AppendLine($"CanShutdown={service.CanShutdown}");
            sb.AppendLine($"CanStop={service.CanStop}");
            sb.AppendLine($"DependentServices={string.Join(", ", service.DependentServices.Select(s => s.ServiceName))}");
            sb.AppendLine($"ServicesDependedOn={string.Join(", ", service.ServicesDependedOn.Select(s => s.ServiceName))}");
            sb.AppendLine($"MachineName={service.MachineName}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Service read failed: {ex.Message}"));
        }
    }
}