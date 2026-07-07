// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         WindowsUpdateReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows Update settings.
/// </summary>
public sealed class WindowsUpdateReadTool : AITool
{
    private const string WindowsUpdateAutoUpdateKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
    private const string WindowsUpdatePolicyKey = "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate";








    private static void ReadKeyValues(RegistryKey root, string keyPath, StringBuilder sb)
    {
        using RegistryKey? key = root.OpenSubKey(keyPath, writable: false);
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
            StringBuilder sb = new();
            dynamic updateSession = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session")!);
            dynamic updateSearcher = updateSession.CreateUpdateSearcher();
            int count = updateSearcher.GetTotalHistoryCount();
            if (count <= 0)
            {
                return Task.FromResult(ToolResult.SuccessResult("No Windows Update history entries found."));
            }

            dynamic history = updateSearcher.QueryHistory(0, Math.Min(count, 50));
            foreach (dynamic entry in history) sb.AppendLine($"Title={entry.Title}, Date={entry.Date}, Operation={entry.Operation}, ResultCode={entry.ResultCode}, HResult={entry.HResult}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Windows Update history listing failed: {ex.Message}"));
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
                return Task.FromResult(ToolResult.SuccessResult("No Windows Update policy settings configured."));
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Windows Update settings read failed: {ex.Message}"));
        }
    }
}