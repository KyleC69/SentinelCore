// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ListDllsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating DLLs loaded into processes using the
///     Sysinternals ListDLLs utility. Loaded-module inventory is a core primitive
///     for DLL injection and DLL hijacking detection.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ListDllsReadTool
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

        string[] lines = output.Split(["\r\n", "\n"], StringSplitOptions.None);
        if (lines.Length <= maxLines)
        {
            return output;
        }

        return string.Join("\n", lines.Take(maxLines)) + $"\n... (truncated to {maxLines} lines)";
    }








    /// <summary>
    ///     Lists DLLs loaded by all processes, or by a specific process when a name
    ///     or PID is supplied.
    /// </summary>
    /// <param name="processNameOrPid">Optional process name (e.g., explorer) or numeric PID to scope the listing.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 300.</param>
    /// <returns>A <see cref="ToolResult" /> containing the loaded-DLL listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_memoryinspection_list_loaded_modules", ReadOnly = true, Destructive = false)]
    [Description("Lists DLLs loaded by processes using Sysinternals ListDLLs. Requires ListDLLs to be installed; full output requires elevation.")]
    public async Task<ToolResult> ListDllsListLoadedAsync([Description("Optional process name (e.g., explorer) or numeric PID to scope the listing.")] string? processNameOrPid = null, [Description("Maximum number of output lines to return. Defaults to 300.")] int maxLines = 300)
    {
        if (processNameOrPid is not null)
        {
            if (string.IsNullOrWhiteSpace(processNameOrPid))
            {
                return ToolResult.Fail("processNameOrPid cannot be empty.", "Input validation");
            }

            // Numeric PIDs and simple image names are the only accepted shapes.
            if (!uint.TryParse(processNameOrPid, out _) && !Regex.IsMatch(processNameOrPid, @"^[a-zA-Z0-9._\- ]+$"))
            {
                return ToolResult.Fail("Invalid process name or PID format", "ListDLLs enumeration");
            }
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        string arguments = string.IsNullOrWhiteSpace(processNameOrPid) ? "-nobanner /accepteula" : $"-nobanner /accepteula \"{processNameOrPid}\"";

        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "listdlls.exe",
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
                return ToolResult.Fail($"ListDLLs failed with exit code {process.ExitCode}: {error.Trim()}", "ListDLLs enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "ListDLLs enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "ListDLLs enumeration failed.");
        }
    }
}
