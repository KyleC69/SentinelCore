// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PsListReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating process and thread detail (CPU time, context
///     switches, thread states) using the Sysinternals PsList utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PsListReadTool
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
    ///     Lists running processes with kernel/user CPU time and thread counts using
    ///     PsList.
    /// </summary>
    /// <param name="processNameOrPid">Optional process name or numeric PID to scope the listing.</param>
    /// <param name="includeThreads">Whether to include per-thread statistics. Defaults to false.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the process listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_execution_list_processes_pslist", ReadOnly = true, Destructive = false)]
    [Description("Lists processes with CPU and memory statistics using Sysinternals PsList. Requires PsList to be installed.")]
    public async Task<ToolResult> PsListListProcessesAsync([Description("Optional process name (e.g., explorer) or numeric PID to scope the listing.")] string? processNameOrPid = null, [Description("Whether to include per-thread statistics. Defaults to false.")] bool includeThreads = false, [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        if (processNameOrPid is not null)
        {
            if (string.IsNullOrWhiteSpace(processNameOrPid))
            {
                return ToolResult.Fail("processNameOrPid cannot be empty.", "Input validation");
            }

            if (!uint.TryParse(processNameOrPid, out _) && !Regex.IsMatch(processNameOrPid, @"^[a-zA-Z0-9._\- ]+$"))
            {
                return ToolResult.Fail("Invalid process name or PID format.", "PsList enumeration");
            }
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        StringBuilder arguments = new("-nobanner /accepteula");
        if (includeThreads)
        {
            arguments.Append(" -t");
        }

        if (!string.IsNullOrWhiteSpace(processNameOrPid))
        {
            arguments.Append(" \"").Append(processNameOrPid).Append('"');
        }

        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "pslist.exe",
                    Arguments = arguments.ToString(),
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
                return ToolResult.Fail($"PsList failed with exit code {process.ExitCode}: {error.Trim()}", "PsList enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "PsList enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PsList enumeration failed.");
        }
    }
}
