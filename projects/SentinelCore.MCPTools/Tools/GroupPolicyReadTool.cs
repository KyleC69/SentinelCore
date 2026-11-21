// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         GroupPolicyReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for inspecting local group policy registry settings.
/// </summary>
[McpServerToolType]
public sealed class GroupPolicyReadTool
{

    private static readonly string[] SPolicyRoots =
    [
            "SOFTWARE\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy"
    ];








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_list_group_policy", ReadOnly = true, Destructive = false)]
    [Description("Lists local group policy keys and values under a policy root path.")]
    public async Task<ToolResult> GroupPolicyListAsync([Description("The policy key path under the policy root, e.g. Microsoft\\Windows.")] string keyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return ToolResult.Fail("keyPath is required", "GroupPolicyReadTool");
            }

            StringBuilder sb = new();
            foreach (string root in SPolicyRoots)
            {
                string fullPath = $"{root}\\{keyPath}";
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(fullPath, false);
                if (key is null)
                {
                    continue;
                }

                sb.AppendLine($"[{fullPath}]");
                foreach (string value in key.GetValueNames())
                {
                    string displayName = string.IsNullOrEmpty(value) ? "(Default)" : value;
                    sb.AppendLine($"  {displayName}={key.GetValue(value)}");
                }

                foreach (string subKey in key.GetSubKeyNames()) sb.AppendLine($"  [SubKey] {subKey}");
            }

            return sb.Length == 0 ? ToolResult.Fail($"No group policy keys found under {keyPath}", "GroupPolicyReadTool") : ToolResult.Ok(sb.ToString(), "GroupPolicyReadTool");

        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "GroupPolicyReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardening_read_group_policy", ReadOnly = true, Destructive = false)]
    [Description("Reads a local group policy registry value from HKLM policy hives.")]
    public async Task<ToolResult> GroupPolicyReadValueAsync([Description("The policy key path under the policy root, e.g. Microsoft\\Windows\\WindowsUpdate.")] string keyPath, [Description("The value name to read.")] string valueName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath) || string.IsNullOrWhiteSpace(valueName))
            {
                return ToolResult.Fail("keyPath and valueName are required", "GroupPolicyReadTool");
            }

            foreach (string root in SPolicyRoots)
            {
                string fullPath = $"{root}\\{keyPath}";
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(fullPath, false);
                if (key is not null)
                {
                    object? value = key.GetValue(valueName);
                    if (value is not null)
                    {
                        return ToolResult.Ok($"Key={fullPath}, ValueName={valueName}, Value={value}", "GroupPolicyReadTool");
                    }
                }
            }

            return ToolResult.Fail($"Group policy value not found: {keyPath}\\{valueName}", "GroupPolicyReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "GroupPolicyReadTool");
        }
    }
}
