// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WmiQueryTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Microsoft.Management.Infrastructure;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for executing CIM/MI WQL queries.
///     Uses Microsoft.Management.Infrastructure instead of legacy System.Management to align with the approved CIM/MI
///     APIs.
/// </summary>
public sealed class WmiQueryTool : AITool
{
    public override string Description { get; } = "Read-only tool for executing CIM/MI WQL queries using Microsoft.Management.Infrastructure.";
    public override string Name { get; } = "WMI_Query";








    [Description("Lists the names of CIM classes in the specified namespace.")]
    public Task<ToolResult> wmi_list_classes([Description("The CIM namespace, e.g. root\\cimv2.")] string nameSpace = @"root\cimv2", [Description("Optional class name prefix filter, e.g. Win32_.")] string? prefix = null)
    {
        try
        {
            StringBuilder sb = new();
            using CimSession? session = CimSession.Create(null);
            foreach (CimClass? cimClass in session.EnumerateClasses(nameSpace))
            {
                string? className = cimClass.CimSystemProperties.ClassName;
                if (!string.IsNullOrWhiteSpace(prefix) && !className.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sb.AppendLine(className);
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"CIM class listing failed: {ex.Message}"));
        }
    }








    [Description("Executes a read-only CIM WQL query and returns the results as JSON.")]
    public Task<ToolResult> wmi_query([Description("The WQL query to execute, e.g. SELECT * FROM Win32_OperatingSystem.")] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(ToolResult.Fail("query is required."));
            }

            List<Dictionary<string, object?>> results = new();
            using CimSession? session = CimSession.Create(null);
            foreach (CimInstance? instance in session.QueryInstances(@"root\cimv2", "WQL", query))
            {
                Dictionary<string, object?> record = new();
                foreach (CimProperty? property in instance.CimInstanceProperties)
                    record[property.Name] = property.Value switch
                    {
                            CimInstance nested => nested.ToString(),
                            Array array => string.Join("|", array.Cast<object>().Select(x => x != null ? x.ToString() != null ? x.ToString() : string.Empty : string.Empty)),
                            _ => property.Value
                    };

                results.Add(record);
            }

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"CIM query failed: {ex.Message}"));
        }
    }
}