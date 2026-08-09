// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         GroupPolicyReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for inspecting local group policy registry settings.
/// </summary>
public sealed class GroupPolicyReadTool : AITool
{

    private static readonly string[] SPolicyRoots =
    [
            "SOFTWARE\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy"
    ];

    public override string Description { get; } = "Read-only tool for inspecting local group policy registry settings.";
    public override string Name { get; } = "Group_Policy_Read";








    [Description("Lists local group policy keys and values under a policy root path.")]
    public Task<ToolResult> group_policy_list([Description("The policy key path under the policy root, e.g. Microsoft\\Windows.")] string keyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath))
                return Task.FromResult(ToolResult.Fail("keyPath is required."));

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

            if (sb.Length == 0)
            {
                return Task.FromResult(ToolResult.Fail($"No group policy keys found under {keyPath}"));
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Group policy listing failed: {ex.Message}"));
        }
    }








    [Description("Reads a local group policy registry value from HKLM policy hives.")]
    public Task<ToolResult> group_policy_read_value([Description("The policy key path under the policy root, e.g. Microsoft\\Windows\\WindowsUpdate.")] string keyPath, [Description("The value name to read.")] string valueName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath) || string.IsNullOrWhiteSpace(valueName))
            {
                return Task.FromResult(ToolResult.Fail("keyPath and valueName are required."));
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
                        return Task.FromResult(ToolResult.Ok($"Key={fullPath}, ValueName={valueName}, Value={value}"));
                    }
                }
            }

            return Task.FromResult(ToolResult.Fail($"Group policy value not found: {keyPath}\\{valueName}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Group policy read failed: {ex.Message}"));
        }
    }
}