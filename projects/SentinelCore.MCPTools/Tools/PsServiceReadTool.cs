// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PsServiceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating Windows services and their configuration
///     using the Sysinternals PsService utility. Complements the managed
///     Service_List and Service_Read tools with Sysinternals-native output.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PsServiceReadTool
{

    /// <summary>
    ///     Limits the output to the specified maximum number of lines.
    /// </summary>
    /// <param name="output">The output to limit.</param>
    /// <param name="maxLines">Maximum number of lines to return.</param>
    /// <returns>The limited output.</returns>
    private static string LimitLines(string output, int maxLines)
    {
        if (string.IsNullOrEmpty(output))
        {
            return output;
        }

        string[] lines = output.Split('\n');
        if (lines.Length <= maxLines)
        {
            return output;
        }

        return string.Join('\n', lines.Take(maxLines)) + $"\n... (truncated to {maxLines} lines)";
    }








    /// <summary>
    ///     Lists installed services with their configuration and status, or the    ///     detailed configuration of a single
    ///     service.
    /// </summary>
    /// <param name="serviceName">Optional service name to inspect in detail. When omitted, all services are listed.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the service listing or detail.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_persistence_list_services_psservice", ReadOnly = true, Destructive = false)]
    [Description("Lists services and their configuration using Sysinternals PsService. Requires PsService to be installed.")]
    public async Task<ToolResult> PsServiceListServicesAsync([Description("Optional service name to inspect in detail. When omitted, all services are listed.")] string? serviceName = null, [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        if (serviceName is not null)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return ToolResult.Fail("serviceName cannot be empty.", "Input validation");
            }

            if (!Regex.IsMatch(serviceName, @"^[a-zA-Z0-9._\- ]+$"))
            {
                return ToolResult.Fail("Invalid service name format.", "PsService enumeration");
            }
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -c renders the output as CSV; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        string arguments = string.IsNullOrWhiteSpace(serviceName) ? "-c -nobanner /accepteula" : $"-c -nobanner /accepteula \"{serviceName}\"";

        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "psservice.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process process = new() { StartInfo = psi };
            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return ToolResult.Fail($"PsService failed with exit code {process.ExitCode}: {error.Trim()}", "PsService enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "PsService enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PsService enumeration failed.");
        }
    }
}
