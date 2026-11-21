// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         WmiQueryTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Management.Infrastructure;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for executing CIM/MI WQL queries.
///     Uses Microsoft.Management.Infrastructure instead of legacy System.Management to align with the approved CIM/MI
///     APIs. Only SELECT queries are permitted; DML and method invocation are blocked.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class WmiQueryTool
{

    /// <summary>
    ///     The set of CIM namespaces that are permitted for queries.
    ///     Restricting namespaces prevents access to dangerous namespaces
    ///     that could expose method invocation surfaces.
    /// </summary>
    private static readonly HashSet<string> AllowedNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
            @"root\cimv2",
            @"root\cimv2\security\MicrosoftVolumeEncryption",
            @"root\cimv2\power",
            @"root\StandardCimv2",
            @"root\virtualization\v2",
            @"root\Microsoft\Windows\Defender",
            @"root\Microsoft\Windows\Storage"
    };








    /// <summary>
    ///     Validates that the provided namespace is in the allowed list.
    /// </summary>
    private static ToolResult? ValidateNamespace(string nameSpace)
    {
        if (string.IsNullOrWhiteSpace(nameSpace))
        {
            return ToolResult.Fail("namespace is required", "WmiQueryTool");
        }

        if (!AllowedNamespaces.Contains(nameSpace))
        {
            return ToolResult.Fail($"Namespace '{nameSpace}' is not permitted. Allowed namespaces: {string.Join(", ", AllowedNamespaces)}", "WmiQueryTool");
        }

        return null;
    }








    [McpServerTool(Name = "sc_utilities_list_wmi_classes", ReadOnly = true, Destructive = false)]
    [Description("Lists CIM class names in the specified namespace. Only pre-approved namespaces are permitted.")]
    public async Task<ToolResult> WmiListClassesAsync([Description("The CIM namespace to query. Allowed: root\\cimv2, root\\cimv2\\security\\MicrosoftVolumeEncryption, root\\cimv2\\power, root\\StandardCimv2, root\\virtualization\\v2, root\\Microsoft\\Windows\\Defender, root\\Microsoft\\Windows\\Storage.")] string nameSpace = @"root\cimv2", [Description("Optional class name prefix filter.")] string? prefix = null, [Description("Maximum number of classes to return. Defaults to 50.")] int maxResults = 50)
    {
        try
        {
            ToolResult? validation = ValidateNamespace(nameSpace);
            if (validation is not null)
            {
                return validation;
            }

            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxResults);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            using CimSession session = CimSession.Create(null);

            IEnumerable<CimClass> classes = session.EnumerateClasses(nameSpace).Where(c => prefix == null || c.CimSystemProperties.ClassName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Take(maxResults).ToList();

            var result = new { Namespace = nameSpace, Count = classes.Count(), Classes = classes.Select(c => c.CimSystemProperties.ClassName).ToList() };

            return ToolResult.Ok(result, "CIM query complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "CIM class listing");
        }
    }








    [McpServerTool(Name = "sc_utilities_query_wmi", ReadOnly = true, Destructive = false)]
    [Description("Executes a read-only CIM WQL SELECT query and returns a compact result set. Only SELECT queries are permitted.")]
    public async Task<ToolResult> WmiQueryAsync([Description("The WQL SELECT query to execute. Only SELECT queries are permitted.")] string query, [Description("Maximum number of rows to return. Defaults to 25.")] int maxRows = 25, [Description("Maximum number of properties per row. Defaults to 10.")] int maxProperties = 10)
    {
        try
        {
            ToolResult? queryValidation = InputValidator.ValidateWqlQuery(query);
            if (queryValidation is not null)
            {
                return queryValidation;
            }

            ToolResult? maxRowsValidation = InputValidator.ValidateMaxRecords(maxRows, "maxRows");
            if (maxRowsValidation is not null)
            {
                return maxRowsValidation;
            }

            ToolResult? maxPropsValidation = InputValidator.ValidateMaxRecords(maxProperties, "maxProperties");
            if (maxPropsValidation is not null)
            {
                return maxPropsValidation;
            }

            using CimSession session = CimSession.Create(null);

            var compactRows = new List<Dictionary<string, object?>>();
            int totalRows = 0;

            foreach (CimInstance instance in session.QueryInstances(@"root\cimv2", "WQL", query))
            {
                totalRows++;

                if (compactRows.Count >= maxRows)
                {
                    continue; // count but don't include
                }

                var row = new Dictionary<string, object?>();

                foreach (CimProperty prop in instance.CimInstanceProperties.Take(maxProperties))
                {
                    object? value = prop.Value switch
                    {
                            CimInstance nested => nested.CimSystemProperties.ClassName,
                            Array array => string.Join(",", array.Cast<object?>().Where(x => x != null)),
                            _ => prop.Value
                    };

                    row[prop.Name] = value;
                }

                compactRows.Add(row);
            }

            var result = new
            {
                    Query = query,
                    TotalRows = totalRows,
                    ReturnedRows = compactRows.Count,
                    MaxRows = maxRows,
                    MaxProperties = maxProperties,
                    Rows = compactRows
            };

            return ToolResult.Ok(result, "CIM query complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "CIM query");
        }
    }
}
