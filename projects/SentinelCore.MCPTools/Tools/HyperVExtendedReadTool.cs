// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         HyperVExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Management.Infrastructure;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Hyper-V virtual machine checkpoints (snapshots)
///     for VM rollback detection in forensic investigations.
/// </summary>
[McpServerToolType]
public sealed class HyperVExtendedReadTool
{

    private const string HyperVNamespace = @"root\virtualization\v2";








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_hyperv_checkpoints", ReadOnly = true, Destructive = false)]
    [Description("Lists Hyper-V virtual machine checkpoints (snapshots) for VM rollback detection.")]
    public async Task<ToolResult> HyperVListCheckpointsAsync([Description("Maximum number of checkpoints to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            using CimSession session = CimSession.Create(null);

            string query = "SELECT ElementName, Description, CreationTime, VirtualSystemName FROM Msvm_VirtualSystemSettingData WHERE SettingType = 5";
            foreach (CimInstance checkpoint in session.QueryInstances(HyperVNamespace, "WQL", query))
            {
                if (results.Count >= maxRecords) break;

                string? elementName = checkpoint.CimInstanceProperties["ElementName"]?.Value?.ToString();
                string? description = checkpoint.CimInstanceProperties["Description"]?.Value?.ToString();
                string? creationTime = checkpoint.CimInstanceProperties["CreationTime"]?.Value?.ToString();
                string? vmName = checkpoint.CimInstanceProperties["VirtualSystemName"]?.Value?.ToString();

                results.Add(new { CheckpointName = elementName ?? "", Description = description ?? "", CreationTime = creationTime ?? "", VMName = vmName ?? "" });
            }

            return ToolResult.Ok(results, "HyperVExtendedReadTool");
        }
        catch (CimException ex) when (ex.Message.Contains("Invalid namespace", StringComparison.OrdinalIgnoreCase) || ex.HResult == -2147023174)
        {
            return ToolResult.Ok("Hyper-V is not installed or the WMI provider is not available. The 'root\\virtualization\\v2' namespace was not found. Please ensure Hyper-V is installed and the Hyper-V WMI provider is enabled.", "HyperVExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "HyperVExtendedReadTool");
        }
    }
}
