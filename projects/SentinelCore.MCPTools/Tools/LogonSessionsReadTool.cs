// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         LogonSessionsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating active logon sessions and the processes
///     running in them using the Sysinternals LogonSessions utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class LogonSessionsReadTool
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
    ///     Lists active logon sessions with their authentication package, SID, and
    ///     associated processes.
    /// </summary>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the logon session listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_list_logon_sessions", ReadOnly = true, Destructive = false)]
    [Description("Lists active logon sessions with authentication details using Sysinternals LogonSessions. Requires LogonSessions to be installed.")]
    public async Task<ToolResult> LogonSessionsListActiveAsync([Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -p lists processes in each session; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "logonsessions.exe",
                    Arguments = "-p -nobanner /accepteula",
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
                return ToolResult.Fail($"LogonSessions failed with exit code {process.ExitCode}: {error.Trim()}", "Logon session enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "Logon session enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Logon session enumeration failed.");
        }
    }
}
