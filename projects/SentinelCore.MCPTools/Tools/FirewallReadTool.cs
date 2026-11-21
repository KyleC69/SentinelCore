// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         FirewallReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Firewall rules and profiles using the netsh
///     native command-line utility.
/// </summary>
[McpServerToolType]
public sealed class FirewallReadTool
{

    /// <summary>
    ///     Builds the netsh arguments for listing firewall rules, applying optional
    ///     direction filter.
    /// </summary>
    private static string BuildListRulesArgs(string? direction)
    {
        StringBuilder args = new("advfirewall firewall show rule name=all");

        if (!string.IsNullOrWhiteSpace(direction))
        {
            args.Append($" dir={direction}");
        }

        return args.ToString();
    }








    /// <summary>
    ///     Filters netsh rule output by profile, keeping only rule blocks that contain
    ///     a Profiles line matching the specified profile.
    /// </summary>
    private static string FilterByProfile(string rawOutput, string profile)
    {
        // netsh outputs rules as blocks separated by blank lines.
        // Each block starts with "Rule Name:" and contains a "Profiles:" line.
        // We keep only blocks whose Profiles line contains the specified profile name.

        string[] blocks = rawOutput.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
        StringBuilder filtered = new();

        foreach (string block in blocks)
        {
            if (!block.Contains("Rule Name:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Check if the Profiles line contains the requested profile
            string[] lines = block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string? profilesLine = lines.FirstOrDefault(l => l.StartsWith("Profiles:", StringComparison.OrdinalIgnoreCase));

            if (profilesLine is not null && profilesLine.Contains(profile, StringComparison.OrdinalIgnoreCase))
            {
                filtered.AppendLine(block.TrimEnd());
                filtered.AppendLine();
            }
        }

        return filtered.ToString();
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_list_firewall_rules", ReadOnly = true, Destructive = false)]
    [Description("Lists Windows Firewall rules with optional profile and direction filters.")]
    public async Task<ToolResult> FirewallListRulesAsync([Description("Optional direction filter: Inbound or Outbound.")] string? direction = null, [Description("Optional profile filter: Domain, Private, Public.")] string? profile = null, [Description("Maximum number of rules to return. Defaults to 50.")] int maxRecords = 50)
    {
        string? normalizedDir = NormalizeDirection(direction);
        string args = BuildListRulesArgs(normalizedDir);

        ToolResult result = RunNetsh(args);
        if (!result.Success)
        {
            return result;
        }

        string output = result.Results?.ToString() ?? string.Empty;

        // Apply profile filter if specified
        string? normalizedProfile = NormalizeProfile(profile);
        if (normalizedProfile is not null)
        {
            output = FilterByProfile(output, normalizedProfile);
        }

        // Apply max records limit
        output = LimitRules(output, maxRecords);

        return ToolResult.Ok(Helpers.CleanOutputLines(output), "FirewallReadTool");
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_firewall_profiles", ReadOnly = true, Destructive = false)]
    [Description("Reads the current firewall profile settings.")]
    public async Task<ToolResult> FirewallReadProfilesAsync()
    {
        ToolResult result = RunNetsh("advfirewall show allprofiles");
        if (!result.Success)
        {
            return result;
        }

        return ToolResult.Ok(Helpers.CleanOutputLines(result.Results?.ToString()), "FirewallReadTool");
    }








    /// <summary>
    ///     Limits the number of rule blocks in netsh output to the specified maximum.
    /// </summary>
    private static string LimitRules(string rawOutput, int maxRecords)
    {
        string[] blocks = rawOutput.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
        StringBuilder limited = new();

        int count = 0;
        foreach (string block in blocks)
        {
            if (!block.Contains("Rule Name:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (count >= maxRecords)
            {
                break;
            }

            limited.AppendLine(block.TrimEnd());
            limited.AppendLine();
            count++;
        }

        return limited.ToString();
    }








    /// <summary>
    ///     Parses the direction filter string into a normalized form for netsh.
    /// </summary>
    internal static string? NormalizeDirection(string? direction)
    {
        return direction?.Trim().ToUpperInvariant() switch
        {
                "INBOUND" => "In",
                "OUTBOUND" => "Out",
                _ => null
        };
    }








    /// <summary>
    ///     Parses the profile filter string into a normalized form for output filtering.
    /// </summary>
    internal static string? NormalizeProfile(string? profile)
    {
        return profile?.Trim().ToUpperInvariant() switch
        {
                "DOMAIN" => "Domain",
                "PRIVATE" => "Private",
                "PUBLIC" => "Public",
                _ => null
        };
    }








    /// <summary>
    ///     Runs a netsh command and returns the standard output, or a failure result if
    ///     the process cannot start or returns a non-zero exit code.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static ToolResult RunNetsh(string arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                    FileName = "netsh",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return ToolResult.Fail("Operation failed", "FirewallReadTool");
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode != 0 ? ToolResult.Fail($"netsh failed: {stderr}", "FirewallReadTool") : ToolResult.Ok(stdout, "FirewallReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "FirewallReadTool");
        }
    }
}
