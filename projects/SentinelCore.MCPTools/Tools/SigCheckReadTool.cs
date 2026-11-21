// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         SigCheckReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for verifying file digital signatures and reputation using
///     the Sysinternals SigCheck utility. Unsigned or revoked binaries are a
///     primary malware indicator.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class SigCheckReadTool
{

    /// <summary>
    ///     Cleans and limits raw SigCheck standard output to the specified maximum number of lines.
    /// </summary>
    /// <param name="output">The raw standard output captured from the SigCheck process.</param>
    /// <param name="maxLines">Maximum number of lines to return.</param>
    /// <returns>
    ///     The cleaned, line-limited output joined with '\n'. Each line is stripped of ANSI escape
    ///     sequences, control characters, and surrounding whitespace, and blank lines are dropped, so
    ///     raw process output never leaks NUL bytes or mixed line endings into the tool result.
    /// </returns>
    private static string LimitLines(string output, int maxLines)
    {
        List<string> lines = Helpers.CleanOutputLines(output);
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        return lines.Count <= maxLines ? string.Join('\n', lines) : string.Join('\n', lines.Take(maxLines)) + $"\n... (truncated to {maxLines} lines)";
    }








    /// <summary>
    ///     Verifies the digital signature of a file and reports certificate details
    ///     using SigCheck.
    /// </summary>
    /// <param name="filePath">The absolute path of the file to verify.</param>
    /// <param name="maxLines">Maximum number of output lines to return. Defaults to 100.</param>
    /// <param name="cancellationToken">Propagates caller cancellation and enforces the 20 second execution cap.</param>
    /// <returns>A <see cref="ToolResult" /> containing the SigCheck verification output.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_cryptography_verify_file_signature", ReadOnly = true, Destructive = false)]
    [Description("Verifies a file's digital signature and reports certificate details using Sysinternals SigCheck. Requires SigCheck to be installed; revocation checks make outbound network calls to certificate authorities.")]
    public async Task<ToolResult> SigCheckVerifyFileAsync([Description("The absolute path of the file to verify.")] string filePath, [Description("Maximum number of output lines to return. Defaults to 100.")] int maxLines = 100, [Description("Cancellation token for the operation.")] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ToolResult.Fail("filePath cannot be empty.", "Input validation");
        }

        if (!File.Exists(filePath))
        {
            return ToolResult.Fail($"File not found: {filePath}", "SigCheck verification");
        }

        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxLines, "maxLines");
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        // -a shows extended version info; -nobanner suppresses the banner; /accepteula suppresses the EULA prompt.
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "sigcheck.exe",
                    Arguments = $"-a -nobanner /accepteula \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process process = new() { StartInfo = psi };
            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                throw new TimeoutException($"sigcheck {filePath} timed out after 20 seconds.");
            }

            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return ToolResult.Fail($"SigCheck failed with exit code {process.ExitCode}: {error.Trim()}", "SigCheck verification");
            }

            // Limit lines
            string limitedOutput = LimitLines(output, maxLines);

            if (limitedOutput.Length == 0)
            {
                return ToolResult.Fail("SigCheck returned no readable output.", "SigCheck verification");
            }

            return ToolResult.Ok(limitedOutput, "SigCheck verification complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "SigCheck verification failed.");
        }
    }
}
