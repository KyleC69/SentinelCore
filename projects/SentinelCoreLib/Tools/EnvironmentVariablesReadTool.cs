// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         EnvironmentVariablesReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Collections;
using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying environment variables.
/// </summary>
public sealed class EnvironmentVariablesReadTool : AITool
{

    [Description("Lists environment variables for the current process, user, or machine.")]
    public Task<ToolResult> environment_list([Description("The target scope: Process, User, or Machine. Defaults to Process.")] EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        try
        {
            IDictionary variables = Environment.GetEnvironmentVariables(target);
            StringBuilder sb = new();
            foreach (System.Collections.DictionaryEntry entry in variables) sb.AppendLine($"{entry.Key}={entry.Value}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Environment variable listing failed: {ex.Message}"));
        }
    }








    [Description("Reads a specific environment variable for the current process, user, or machine.")]
    public Task<ToolResult> environment_read_value([Description("The name of the environment variable.")] string variableName, [Description("The target scope: Process, User, or Machine. Defaults to Process.")] EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return Task.FromResult(ToolResult.FailureResult("variableName is required."));
            }

            string? value = Environment.GetEnvironmentVariable(variableName, target);
            if (value is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Environment variable not found: {variableName} ({target})"));
            }

            return Task.FromResult(ToolResult.SuccessResult($"{variableName}={value}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Environment variable read failed: {ex.Message}"));
        }
    }
}