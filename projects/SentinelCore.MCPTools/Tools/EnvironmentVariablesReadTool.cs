// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         EnvironmentVariablesReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Collections;
using System.ComponentModel;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying environment variables.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class EnvironmentVariablesReadTool
{

    [McpServerTool(Name = "sc_configuration_list_env_variables", ReadOnly = true, Destructive = false)]
    [Description("Lists environment variables for the current process, user, or machine.")]
    public async Task<ToolResult> EnvironmentListAsync([Description("The target scope: Process, User, or Machine. Defaults to Process.")] EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, [Description("Maximum number of variables to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            IDictionary variables = Environment.GetEnvironmentVariables(target);
            List<object> results = new();
            int count = 0;
            foreach (DictionaryEntry entry in variables)
            {
                if (count >= maxRecords)
                {
                    break;
                }

                results.Add(new { Name = Helpers.CleanValue(entry.Key?.ToString()), Value = Helpers.CleanValue(entry.Value?.ToString()) });
                count++;
            }

            return ToolResult.Ok(results, "EnvironmentVariablesReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "EnvironmentVariablesReadTool");
        }
    }








    [McpServerTool(Name = "sc_configuration_read_env_variable", ReadOnly = true, Destructive = false)]
    [Description("Reads a specific environment variable for the current process, user, or machine.")]
    public async Task<ToolResult> EnvironmentReadValueAsync([Description("The name of the environment variable.")] string variableName, [Description("The target scope: Process, User, or Machine. Defaults to Process.")] EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return ToolResult.Fail("variableName is required", "EnvironmentVariablesReadTool");
            }

            string? value = Environment.GetEnvironmentVariable(variableName, target);
            return value is null ? ToolResult.Fail($"Environment variable not found: {variableName} ({target})", "EnvironmentVariablesReadTool") : ToolResult.Ok(new { Name = variableName, Value = Helpers.CleanValue(value) }, "EnvironmentVariablesReadTool");

        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "EnvironmentVariablesReadTool");
        }
    }
}
