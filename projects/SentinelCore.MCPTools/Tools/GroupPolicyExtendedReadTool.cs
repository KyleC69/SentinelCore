// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         GroupPolicyExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Group Policy Resultant Set of Policy (RSOP)
///     for effective policy analysis via the RSOP WMI namespace.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class GroupPolicyExtendedReadTool
{

    /// <summary>
    ///     Reads the Resultant Set of Policy (RSOP) for the computer via the RSOP WMI namespace.
    /// </summary>
    /// <param name="maxRecords">Maximum number of policy entries to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing JSON-formatted RSOP entries.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_rsop", ReadOnly = true, Destructive = false)]
    [Description("Reads the Resultant Set of Policy (RSOP) applied to the computer via the WMI RSOP namespace.")]
    public async Task<ToolResult> GroupPolicyReadRsopAsync(
     [Description("The namespace for the query defaults to:root\\rsop\\computer")] string rsopNamespace = @"root\rsop\computer",
       [Description("The WMI Query")] string query = "SELECT * FROM RSOP_GPLink",
            int maxRecords = 50)
    {
        try
        {
            ToolResult? validation = InputValidator.ValidateMaxRecords(maxRecords);
            if (validation is not null)
            {
                return validation;
            }

            List<Dictionary<string, object?>> records = new();

            using ManagementObjectSearcher searcher = new(rsopNamespace, query);

            foreach (ManagementObject obj in searcher.Get())
            {
                if (records.Count >= maxRecords)
                {
                    break;
                }

                Dictionary<string, object?> record = new(
                        StringComparer.OrdinalIgnoreCase);

                foreach (PropertyData property in obj.Properties)
                {
                    record[property.Name] = property.Value;
                }

                records.Add(record);
            }

            return ToolResult.Ok(new
            {
                RecordCount = records.Count,
                Namespace = rsopNamespace,
                Query = query,
                Records = records
            }, "GroupPolicyReadTool");
        }
        catch (ManagementException ex)
        {
            return ToolResult.Fail(
                    $"WMI query failed: {ex.Message}",
                    "RSOP read");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "RSOP read");
        }
    }
}
