// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         SessionHelper.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Diagnostics;
using System.Runtime.Versioning;




namespace SentinelCoreMCP.Tools.Interop;





/// <summary>
///     Shared utility for enumerating Windows terminal sessions.
///     Uses the built-in <c>query session</c> utility (a documented Windows Sysinternals-style
///     diagnostic command) instead of direct P/Invoke into wtsapi32.dll, per spec §6.2.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class SessionHelper
{

    /// <summary>
    ///     Enumerates active terminal sessions on the local machine using the built-in
    ///     <c>query session</c> command, executed asynchronously.
    /// </summary>
    /// <param name="maxRecords">Maximum number of session records to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing typed session records.</returns>
    internal static async Task<ToolResult> EnumerateSessionsAsync(int maxRecords = 50)
    {
        try
        {
            List<SessionRecord> results = new();

            ProcessStartInfo psi = new()
            {
                    FileName = "query.exe",
                    Arguments = "session",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process process = new() { StartInfo = psi };
            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            bool exited = await Task.Run(() => process.WaitForExit(30_000)).ConfigureAwait(false);
            if (!exited)
            {
                process.Kill();
                return ToolResult.Fail("query session timed out after 30 seconds.", "Session enumeration");
            }

            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return ToolResult.Fail($"query session failed with exit code {process.ExitCode}: {error.Trim()}", "Session enumeration");
            }

            // Format: SESSIONNAME  USERNAME  ID  STATE  TYPE  DEVICE
            string[] lines = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (results.Count >= maxRecords)
                {
                    break;
                }

                string trimmed = line.Trim();

                // Skip header and separator lines
                if (trimmed.StartsWith("SESSIONNAME", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("====", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    continue;
                }

                // The session name can be blank; locate the numeric session ID column.
                int idIndex = -1;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i], out _))
                    {
                        idIndex = i;
                        break;
                    }
                }

                if (idIndex < 0)
                {
                    continue;
                }

                results.Add(new SessionRecord(SessionName: idIndex > 0 ? parts[0] : string.Empty, UserName: idIndex > 1 ? string.Join(" ", parts[1..idIndex]) : string.Empty, SessionId: parts[idIndex], State: idIndex + 1 < parts.Length ? parts[idIndex + 1] : string.Empty, Type: idIndex + 2 < parts.Length ? parts[idIndex + 2] : string.Empty));
            }

            return ToolResult.Ok(results, $"Enumerated {results.Count} session(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Session enumeration");
        }
    }








    /// <summary>
    ///     A single terminal session record.
    /// </summary>
    /// <param name="SessionName">The session name (e.g., console, rdp-tcp#0).</param>
    /// <param name="UserName">The user logged into the session, if any.</param>
    /// <param name="SessionId">The numeric session identifier.</param>
    /// <param name="State">The session state (Active, Disconnected, Listen, etc.).</param>
    /// <param name="Type">The session type.</param>
    public sealed record SessionRecord(string SessionName, string UserName, string SessionId, string State, string Type);
}