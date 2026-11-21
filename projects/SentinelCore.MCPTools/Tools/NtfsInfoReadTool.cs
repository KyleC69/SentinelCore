// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         NtfsInfoReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for reporting NTFS volume information (cluster size, MFT
///     records, journal state) using the Sysinternals NTFSInfo utility.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class NtfsInfoReadTool
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
    ///     Reports NTFS volume geometry and metadata for the specified drive letter.
    /// </summary>
    /// <param name="driveLetter">The drive letter to inspect, e.g. C. Defaults to the system drive.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 200.</param>
    /// <returns>A <see cref="ToolResult" /> containing the NTFS volume report.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_read_ntfs_volume", ReadOnly = true, Destructive = false)]
    [Description("Reports NTFS volume geometry, MFR size, and metadata file layout using Sysinternals NTFSInfo.")]
    public async Task<ToolResult> NtfsInfoReadVolumeAsync([Description("The drive letter to inspect, e.g. C.")] string? driveLetter = "c:", [Description("Maximum number of output lines to return. Defaults to 200.")] int maxLines = 200)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        string drive = string.IsNullOrWhiteSpace(driveLetter) ? Environment.SystemDirectory[..2] : driveLetter.TrimEnd(':').ToUpperInvariant();

        if (drive.Length != 1 || drive[0] is < 'A' or > 'Z')
        {
            return ToolResult.Fail($"Invalid drive letter: {drive}. A single letter A-Z is required.", "NTFSInfo volume read");
        }

        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "ntfsinfo.exe",
                Arguments = $"{driveLetter}: -nobanner /accepteula",
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
                return ToolResult.Fail($"NTFSInfo failed with exit code {process.ExitCode}: {error.Trim()}", "NTFSInfo volume read");
            }

            // Limit lines
            string limitedOutput = LimitLines(output, maxLines);

            return ToolResult.Ok(limitedOutput, "NTFSInfo volume read complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NTFSInfo volume read failed.");
        }
    }
}
