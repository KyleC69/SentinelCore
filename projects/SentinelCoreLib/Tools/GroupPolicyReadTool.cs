// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         GroupPolicyReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for inspecting local group policy registry settings.
/// </summary>
public sealed class GroupPolicyReadTool : AITool
{
    private static readonly string[] s_policyRoots =
    [
            "SOFTWARE\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies",
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy"
    ];








    [Description("Lists local group policy keys and values under a policy root path.")]
    public Task<ToolResult> group_policy_list([Description("The policy key path under the policy root, e.g. Microsoft\\Windows.")] string keyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                return Task.FromResult(ToolResult.FailureResult("keyPath is required."));
            }

            StringBuilder sb = new();
            foreach (string root in s_policyRoots)
            {
                string fullPath = $"{root}\\{keyPath}";
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(fullPath, writable: false);
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
                return Task.FromResult(ToolResult.FailureResult($"No group policy keys found under {keyPath}"));
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Group policy listing failed: {ex.Message}"));
        }
    }








    [Description("Reads a local group policy registry value from HKLM policy hives.")]
    public Task<ToolResult> group_policy_read_value([Description("The policy key path under the policy root, e.g. Microsoft\\Windows\\WindowsUpdate.")] string keyPath, [Description("The value name to read.")] string valueName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyPath) || string.IsNullOrWhiteSpace(valueName))
            {
                return Task.FromResult(ToolResult.FailureResult("keyPath and valueName are required."));
            }

            foreach (string root in s_policyRoots)
            {
                string fullPath = $"{root}\\{keyPath}";
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(fullPath, writable: false);
                if (key is not null)
                {
                    object? value = key.GetValue(valueName);
                    if (value is not null)
                    {
                        return Task.FromResult(ToolResult.SuccessResult($"Key={fullPath}, ValueName={valueName}, Value={value}"));
                    }
                }
            }

            return Task.FromResult(ToolResult.FailureResult($"Group policy value not found: {keyPath}\\{valueName}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Group policy read failed: {ex.Message}"));
        }
    }
}