// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ProcDumpReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Tooling surface for the Sysinternals ProcDump utility.
///     ProcDump writes dump files to disk, which is a state-changing side effect;
///     the capture operation is honestly declared as non-read-only (spec §4.3, §7.1).
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ProcDumpReadTool
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
    ///     Captures a full user-mode dump of a process to a caller-specified file
    ///     path. This operation writes a file and is therefore not read-only.
    /// </summary>
    /// <param name="processId">The numeric PID of the process to dump.</param>
    /// <param name="dumpFilePath">The absolute .dmp file path to write. The containing directory must exist.</param>
    /// <returns>A <see cref="ToolResult" /> containing the ProcDump console output.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_memoryinspection_capture_process_dump", ReadOnly = false, Destructive = false)]
    [Description("Captures a full process dump to a specified file path using Sysinternals ProcDump. Writes a dump file; requires ProcDump to be installed and elevation for most processes.")]
    public async Task<ToolResult> ProcDumpCaptureDumpAsync([Description("The numeric PID of the process to dump.")] int processId, [Description("The absolute .dmp file path to write. The containing directory must exist.")] string dumpFilePath)
    {
        ToolResult? pidValidation = InputValidator.ValidatePositive(processId, "processId");
        if (pidValidation is not null)
        {
            return pidValidation;
        }

        if (string.IsNullOrWhiteSpace(dumpFilePath))
        {
            return ToolResult.Fail("dumpFilePath cannot be empty.", "Input validation");
        }

        if (!Path.IsPathRooted(dumpFilePath) || !dumpFilePath.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("dumpFilePath must be an absolute path ending with .dmp", "ProcDump capture");
        }

        string? directory = Path.GetDirectoryName(dumpFilePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return ToolResult.Fail($"Dump directory does not exist: {directory}", "ProcDump capture");
        }

        // -ma captures full memory; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "procdump.exe",
                    Arguments = $"-ma -nobanner /accepteula {processId} \"{dumpFilePath}\"",
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

            // ProcDump's exit code is not a reliable success signal: it returns -2 after writing a dump and -1
            // for bad arguments, while also writing usage text to stdout. The dump file itself is the only
            // dependable indicator that the capture actually succeeded.
            FileInfo dumpFile = new(dumpFilePath);
            if (!dumpFile.Exists || dumpFile.Length == 0)
            {
                string detail = string.IsNullOrWhiteSpace(error) ? Helpers.CleanValue(output) : Helpers.CleanValue(error);
                return ToolResult.Fail($"ProcDump did not create a dump file (exit code {process.ExitCode}): {detail}", "ProcDump capture");
            }

            // Limit lines
            string limitedOutput = LimitLines(output, 100);

            return ToolResult.Ok(string.IsNullOrWhiteSpace(limitedOutput) ? $"Dump written to {dumpFilePath} ({dumpFile.Length} bytes)." : limitedOutput, "ProcDump capture complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "ProcDump capture failed.");
        }
    }
}
