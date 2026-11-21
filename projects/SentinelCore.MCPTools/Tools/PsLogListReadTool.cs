// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PsLogListReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for dumping event log records using the Sysinternals
///     PsLogList utility. Complements the managed Event_Log_Query tool with
///     Sysinternals-native formatting.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PsLogListReadTool
{

    /// <summary>
    ///     Cleans and limits raw PsLogList standard output to the specified maximum number of lines.
    /// </summary>
    /// <param name="output">The raw standard output captured from the PsLogList process.</param>
    /// <param name="maxLines">Maximum number of lines to return.</param>
    /// <returns>
    ///     The cleaned, line-limited output joined with '\n'. Each line is stripped of ANSI escape
    ///     sequences, control characters, and surrounding whitespace, and blank lines are dropped, so
    ///     raw process output never leaks NUL bytes, lone carriage returns, or mixed line endings into
    ///     the tool result.
    /// </returns>
    private static string LimitLines(string output, int maxLines)
    {
        List<string> lines = Helpers.CleanOutputLines(output);
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        return lines.Count <= maxLines
                ? string.Join('\n', lines)
                : string.Join('\n', lines.Take(maxLines)) + $"\n... (truncated to {maxLines} lines)";
    }








    /// <summary>
    ///     Dumps recent events from an event log channel using PsLogList.
    /// </summary>
    /// <param name="logName">The event log name to dump, e.g. Application, System, Security.</param>
    /// <param name="maxEvents">Maximum number of events to dump. Defaults to 50.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the event dump.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_dump_event_log", ReadOnly = true, Destructive = false)]
    [Description("Dumps recent event log records using Sysinternals PsLogList. Requires PsLogList to be installed; the Security log requires elevation.")]
    public async Task<ToolResult> PsLogListDumpEventsAsync([Description("The event log name to dump, e.g. Application, System, Security.")] string logName, [Description("Maximum number of events to dump. Defaults to 50.")] int maxEvents = 50, [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        if (string.IsNullOrWhiteSpace(logName))
        {
            return ToolResult.Fail("logName cannot be empty.", "Input validation");
        }

        // Log names are restricted to a safe character set to prevent argument shaping.
        if (!Regex.IsMatch(logName, @"^[a-zA-Z0-9 _\-]+$"))
        {
            return ToolResult.Fail("Invalid log name format.", "PsLogList dump");
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxEvents, "maxEvents");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        ToolResult? maxLinesValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxLinesValidation is not null)
        {
            return maxLinesValidation;
        }

        // -n limits the event count; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "psloglist.exe",
                    Arguments = $"-n {maxEvents} -nobanner /accepteula \"{logName}\"",
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
                return ToolResult.Fail($"PsLogList failed with exit code {process.ExitCode}: {error.Trim()}", "PsLogList dump");
            }

            // Limit lines
            string limitedOutput = LimitLines(output, maxLines);

            if (limitedOutput.Length == 0)
            {
                return ToolResult.Fail("PsLogList returned no readable output.", "PsLogList dump");
            }

            return ToolResult.Ok(limitedOutput, "PsLogList dump complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PsLogList dump failed.");
        }
    }
}
