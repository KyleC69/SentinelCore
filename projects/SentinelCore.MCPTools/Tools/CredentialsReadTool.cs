// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         CredentialsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating generic credential targets stored in Windows Credential Manager.
///     Uses the built-in <c>cmdkey /list</c> utility instead of direct P/Invoke into advapi32.dll,
///     per spec §6.2. Only target names and metadata are returned; no secrets are exposed.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class CredentialsReadTool
{

    /// <summary>
    ///     Lists the names (targets) of stored Windows credentials without reading passwords.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing typed credential target records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_list_credentials", ReadOnly = true, Destructive = false)]
    [Description("Lists the names (targets) of stored Windows credentials without reading passwords.")]
    public async Task<ToolResult> CredentialListTargetsAsync()
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                    FileName = "cmdkey.exe",
                    Arguments = "/list",
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
                return ToolResult.Fail("cmdkey process timed out after 30 seconds.", "CredentialsReadTool");
            }

            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return ToolResult.Fail($"cmdkey /list failed with exit code {process.ExitCode}: {error.Trim()}", "Credential target listing");
            }

            List<CredentialTargetRecord> results = new();

            // cmdkey /list output blocks look like:
            //     Target: Domain:target=TERMSRV/host
            //     Type: Domain Password
            //     User: DOMAIN\user
            string[] blocks = output.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (string block in blocks)
            {
                string? target = null;
                string? user = null;
                string? type = null;

                foreach (string rawLine in block.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
                {
                    string line = rawLine.Trim();
                    if (line.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                    {
                        target = line["Target:".Length..].Trim();
                    }
                    else if (line.StartsWith("User:", StringComparison.OrdinalIgnoreCase))
                    {
                        user = line["User:".Length..].Trim();
                    }
                    else if (line.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                    {
                        type = line["Type:".Length..].Trim();
                    }
                }

                if (!string.IsNullOrWhiteSpace(target))
                {
                    results.Add(new CredentialTargetRecord(target, user ?? string.Empty, type ?? string.Empty));
                }
            }

            return ToolResult.Ok(results, $"Enumerated {results.Count} credential target(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Credential target listing");
        }
    }








    /// <summary>
    ///     A single stored credential record (metadata only, no secrets).
    /// </summary>
    /// <param name="Target">The credential target name.</param>
    /// <param name="UserName">The stored user name, if present.</param>
    /// <param name="Type">The credential type (e.g., Domain, Generic).</param>
    public sealed record CredentialTargetRecord(string Target, string UserName, string Type);
}
