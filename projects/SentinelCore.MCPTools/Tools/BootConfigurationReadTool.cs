// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         BootConfigurationReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying system boot configuration using bcdedit.exe.
///     There is no managed .NET API for BCD; shelling bcdedit is the accepted approach.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class BootConfigurationReadTool
{

    /// <summary>
    ///     Returns the current boot entry GUID from the BCD store.
    /// </summary>
    /// <param name="cancellationToken">Propagates caller cancellation and enforces the 20 second execution cap.</param>
    /// <returns>A <see cref="ToolResult" /> containing the bcdedit output.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_boot_current", ReadOnly = true, Destructive = false)]
    [Description("Returns the current boot entry GUID from the BCD store.")]
    public async Task<ToolResult> BcdeditCurrentAsync([Description("Cancellation token for the operation.")] CancellationToken cancellationToken = default)
    {
        try
        {
            string output = await RunBcdeditAsync("/enum {current}", cancellationToken).ConfigureAwait(false);
            return ToolResult.Ok(Helpers.CleanOutputLines(output), "BCD current boot entry read.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BCD current entry read");
        }
    }








    /// <summary>
    ///     Enumerates the active boot configuration store entries.
    /// </summary>
    /// <param name="cancellationToken">Propagates caller cancellation and enforces the 20 second execution cap.</param>
    /// <returns>A <see cref="ToolResult" /> containing the bcdedit output.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_boot_enum", ReadOnly = true, Destructive = false)]
    [Description("Enumerates the active boot configuration store entries.")]
    public async Task<ToolResult> BcdeditEnumAsync([Description("Cancellation token for the operation.")] CancellationToken cancellationToken = default)
    {
        try
        {
            string output = await RunBcdeditAsync("/enum", cancellationToken).ConfigureAwait(false);
            return ToolResult.Ok(Helpers.CleanOutputLines(output), "BCD enumeration complete.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "BCD enumeration");
        }
    }








    /// <summary>
    ///     Runs bcdedit.exe with the specified arguments and returns the standard output asynchronously.
    /// </summary>
    /// <param name="arguments">The arguments to pass to bcdedit.exe.</param>
    /// <param name="cancellationToken">Token propagated from the calling tool operation.</param>
    /// <returns>The standard output of bcdedit.exe.</returns>
    /// <exception cref="TimeoutException">Thrown when bcdedit times out.</exception>
    /// <exception cref="InvalidOperationException">Thrown when bcdedit fails with a non-zero exit code.</exception>
    private static async Task<string> RunBcdeditAsync(string arguments, CancellationToken cancellationToken)
    {
        using Process process = new()
        {
                StartInfo = new ProcessStartInfo
                {
                        FileName = "bcdedit.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                }
        };

        process.Start();

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource exitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        exitCts.CancelAfter(TimeSpan.FromSeconds(20));

        try
        {
            await process.WaitForExitAsync(exitCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            throw new TimeoutException($"bcdedit {arguments} timed out after 20 seconds.");
        }

        string output = await outputTask.ConfigureAwait(false);
        string error = await errorTask.ConfigureAwait(false);

        // Any non-zero exit code is a failure regardless of where the message was written.
        // bcdedit can emit access-denied text on stdout when redirected, so checking the
        // output for emptiness is not enough; it masked "Access is denied." as a success.
        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(output) ? error : output;
            throw new InvalidOperationException($"bcdedit {arguments} failed with exit code {process.ExitCode}: {detail.Trim()}");
        }

        return string.IsNullOrWhiteSpace(output) ? error : output;
    }
}
