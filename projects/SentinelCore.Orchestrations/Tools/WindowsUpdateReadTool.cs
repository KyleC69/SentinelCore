// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WindowsUpdateReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows Update settings.
/// </summary>
public sealed class WindowsUpdateReadTool : AITool
{

    private const string WindowsUpdateAutoUpdateKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
    private const string WindowsUpdatePolicyKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate";
    public override string Description { get; } = "Read-only tool for querying Windows Update settings.";
    public override string Name { get; } = "Windows_Update_Read";








    private static void ReadKeyValues(RegistryKey root, string keyPath, StringBuilder sb)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, false);
        if (key is null)
        {
            return;
        }

        sb.AppendLine($"[{keyPath}]");
        foreach (string valueName in key.GetValueNames())
        {
            string displayName = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName;
            sb.AppendLine($"  {displayName}={key.GetValue(valueName)}");
        }
    }








    [Description("Lists installed Windows update history using the COM UpdateSession.")]
    public Task<ToolResult> windows_update_list_history()
    {
        try
        {
            return Task.FromResult(ToolResult.Fail("Windows Update history listing is not yet implemented."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Windows Update history listing failed: {ex.Message}"));
        }
    }








    [Description("Reads Windows Update policy settings from the registry.")]
    public Task<ToolResult> windows_update_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            ReadKeyValues(Registry.LocalMachine, WindowsUpdatePolicyKey, sb);
            ReadKeyValues(Registry.LocalMachine, WindowsUpdateAutoUpdateKey, sb);

            if (sb.Length == 0)
            {
                return Task.FromResult(ToolResult.Ok("No Windows Update policy settings configured."));
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Windows Update settings read failed: {ex.Message}"));
        }
    }
}