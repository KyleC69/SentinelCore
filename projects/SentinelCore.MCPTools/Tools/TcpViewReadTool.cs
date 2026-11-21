// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         TcpViewReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating TCP/UDP endpoints using Tcpvcon, the
///     command-line companion of the Sysinternals TcpView utility. Endpoint
///     attribution to owning processes is a core network forensics primitive.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class TcpViewReadTool
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
    ///     Lists active TCP/UDP endpoints with owning process attribution using    ///     Tcpvcon.
    /// </summary>
    /// <param name="includeUdp">Whether to include UDP endpoints. Defaults to true.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the endpoint listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_list_endpoints", ReadOnly = true, Destructive = false)]
    [Description("Lists active TCP/UDP endpoints with owning process attribution using Sysinternals Tcpvcon. Requires Tcpvcon to be installed; process attribution requires elevation.")]
    public async Task<ToolResult> TcpViewListEndpointsAsync([Description("Whether to include UDP endpoints. Defaults to true.")] bool includeUdp = true, [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -a includes all endpoint states, -c renders CSV, -n resolves addresses;
        // -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        string arguments = includeUdp ? "-a -c -n -nobanner /accepteula" : "-a -c -n -p -nobanner /accepteula";

        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "tcpvcon.exe",
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
                return ToolResult.Fail($"Tcpvcon failed with exit code {process.ExitCode}: {error.Trim()}", "Tcpvcon endpoint enumeration");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "Tcpvcon endpoint enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Tcpvcon endpoint enumeration failed.");
        }
    }
}
