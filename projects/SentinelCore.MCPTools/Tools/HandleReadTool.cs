// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         HandleReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating open file and kernel object handles using
///     the Sysinternals Handle utility. Handle enumeration is a core forensic
///     primitive for detecting file-locking malware and resource leaks.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class HandleReadTool
{

    /// <summary>
    ///     Lists open handles for all processes, optionally filtered by a search
    ///     substring matched against handle names.
    /// </summary>
    /// <param name="searchFilter">Optional substring to match against handle names (e.g., a file path fragment).</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the handle listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_memoryinspection_list_handles", ReadOnly = true, Destructive = false)]
    [Description("Lists open file and kernel object handles using Sysinternals Handle. Requires Handle to be installed; handle enumeration requires elevation.")]
    public async Task<ToolResult> HandleListAsync([Description("Optional substring to match against handle names, e.g. a file path fragment.")] string? searchFilter = null, [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        // Validate searchFilter if provided
        if (searchFilter is not null && string.IsNullOrWhiteSpace(searchFilter))
        {
            return ToolResult.Fail("searchFilter cannot be empty.", "Input validation");
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -a dumps all handle types; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        StringBuilder arguments = new("-a -nobanner /accepteula");
        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            arguments.Append(" \"").Append(searchFilter).Append('"');
        }

        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "handle.exe",
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
                return ToolResult.Fail($"Handle failed with exit code {process.ExitCode}: {error.Trim()}", "Handle enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "Handle enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Handle enumeration failed.");
        }
    }








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

        string[] lines = output.Split(["\r\n", "\n"], StringSplitOptions.None);
        if (lines.Length <= maxLines)
        {
            return output;
        }

        return string.Join("\n", lines.Take(maxLines)) + $"\n... (truncated to {maxLines} lines)";
    }
}
