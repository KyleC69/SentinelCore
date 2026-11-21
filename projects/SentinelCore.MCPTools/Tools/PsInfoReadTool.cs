// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PsInfoReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating system information (OS version, kernel,
///     install date, hotfixes) using the Sysinternals PsInfo utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PsInfoReadTool
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
    ///     Reports operating system, kernel, install date, and hotfix information
    ///     using PsInfo.
    /// </summary>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 100.</param>
    /// <returns>A <see cref="ToolResult" /> containing the PsInfo system report.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_os_info", ReadOnly = true, Destructive = false)]
    [Description("Reports OS version, kernel build, install date, and hotfixes using Sysinternals PsInfo. Requires PsInfo to be installed.")]
    public async Task<ToolResult> PsInfoReadSystemAsync([Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -h lists hotfixes; -s lists installed software; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "psinfo.exe",
                    Arguments = "-h -nobanner /accepteula",
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
                return ToolResult.Fail($"PsInfo failed with exit code {process.ExitCode}: {error.Trim()}", "PsInfo system read");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "PsInfo system read complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PsInfo system read failed.");
        }
    }
}
