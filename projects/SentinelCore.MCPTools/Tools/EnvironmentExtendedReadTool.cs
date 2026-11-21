// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         EnvironmentExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for analyzing the system PATH environment variable
///     for hijack detection and misconfiguration.
///     Uses ACL inspection instead of creating test files, so the tool has no side effects.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class EnvironmentExtendedReadTool
{

    /// <summary>
    ///     Reads and analyzes the system and user PATH environment variables for hijack detection.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing JSON-formatted PATH analysis.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_env_path", ReadOnly = true, Destructive = false)]
    [Description("Reads and analyzes the system and user PATH environment variables for hijack detection.")]
    public async Task<ToolResult> EnvironmentReadPathAsync()
    {
        try
        {
            List<object> results = new();
            string[] systemPathEntries = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
            string[] userPathEntries = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
            string[] processPathEntries = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

            foreach (string entry in systemPathEntries)
            {
                bool exists = Directory.Exists(entry);
                bool isWritable = exists && IsDirectoryWritable(entry);

                results.Add(new { Path = entry, Source = "System", Exists = exists, IsWritable = isWritable });
            }

            foreach (string entry in userPathEntries)
            {
                bool exists = Directory.Exists(entry);
                bool isWritable = exists && IsDirectoryWritable(entry);

                results.Add(new { Path = entry, Source = "User", Exists = exists, IsWritable = isWritable });
            }

            // Check for duplicates
            var duplicates = processPathEntries.GroupBy(p => p, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            var analysis = new { Entries = results, DuplicatePaths = duplicates, TotalSystemPaths = systemPathEntries.Length, TotalUserPaths = userPathEntries.Length };

            return ToolResult.Ok(analysis, "PATH analysis complete.");

        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PATH analysis");
        }
    }








    /// <summary>
    ///     Determines whether the current user has write access to a directory by inspecting
    ///     its ACL, without creating any files.
    /// </summary>
    /// <param name="directoryPath">The directory to check.</param>
    /// <returns><c>true</c> if the current user has write access; otherwise <c>false</c>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool IsDirectoryWritable(string directoryPath)
    {
        try
        {
            DirectorySecurity security = new DirectoryInfo(directoryPath).GetAccessControl();
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);

            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>())
            {
                if (!rule.FileSystemRights.HasFlag(FileSystemRights.Write) && !rule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles))
                {
                    continue;
                }

                if (rule.AccessControlType == AccessControlType.Deny && (identity.User is null || rule.IdentityReference.Value == identity.User.Value))
                {
                    return false;
                }

                if (rule.AccessControlType == AccessControlType.Allow && (identity.User is not null && rule.IdentityReference.Value == identity.User.Value || principal.IsInRole(WindowsBuiltInRole.Administrator) && rule.IdentityReference.Value.Contains("Administrators", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
