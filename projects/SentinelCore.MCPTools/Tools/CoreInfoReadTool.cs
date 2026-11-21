// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         CoreInfoReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating low-level system information (CPU topology,
///     cache geometry, NUMA nodes, virtualization) using the Sysinternals Coreinfo
///     utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class CoreInfoReadTool
{

    /// <summary>
    ///     Enumerates processor topology, cache geometry, NUMA nodes, and
    ///     virtualization support using Coreinfo.
    /// </summary>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the Coreinfo system report.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_cpu_topology", ReadOnly = true, Destructive = false)]
    [Description("Enumerates CPU topology, cache geometry, NUMA nodes, and virtualization support using Sysinternals Coreinfo. Requires Coreinfo to be installed.")]
    public async Task<ToolResult> CoreInfoReadSystemAsync([Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -c processor cores, -v virtualization; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "coreinfo.exe",
                    Arguments = "-c -v -nobanner /accepteula",
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
                return ToolResult.Fail($"Coreinfo failed with exit code {process.ExitCode}: {error.Trim()}", "Coreinfo system read");
            }

            // Limit lines and normalize the output into a clean line array
            List<string> limitedOutput = Helpers.CleanOutputLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "Coreinfo system read complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Coreinfo system read failed.");
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
