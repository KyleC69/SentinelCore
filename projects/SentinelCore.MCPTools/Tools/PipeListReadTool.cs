// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PipeListReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating named pipes on the local system using the
///     Sysinternals PipeList utility. Unusual named pipes are a common
///     local-IPC backdoor mechanism, making this a high-value forensic surface.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PipeListReadTool
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
    ///     Lists named pipes with their instances and maximum instance counts using
    ///     PipeList.
    /// </summary>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the named pipe listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_list_pipes", ReadOnly = true, Destructive = false)]
    [Description("Lists named pipes with instance counts using Sysinternals PipeList. Requires PipeList to be installed.")]
    public async Task<ToolResult> PipeListListPipesAsync([Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "pipelist.exe",
                    Arguments = "-nobanner /accepteula",
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
                return ToolResult.Fail($"PipeList failed with exit code {process.ExitCode}: {error.Trim()}", "Named pipe enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "Named pipe enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Named pipe enumeration failed.");
        }
    }
}
