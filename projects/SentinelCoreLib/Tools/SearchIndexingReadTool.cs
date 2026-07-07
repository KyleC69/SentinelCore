// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SearchIndexingReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows Search indexing options.
/// </summary>
public sealed class SearchIndexingReadTool : AITool
{
    private const string CrawlScopeKey = "SOFTWARE\\Microsoft\\Windows Search\\CrawlScopeManager";
    private const string WindowsSearchKey = "SOFTWARE\\Microsoft\\Windows Search";








    [Description("Lists indexed locations from the Windows Search crawl scope registry.")]
    public Task<ToolResult> search_indexing_list_scopes()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CrawlScopeKey, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Crawl scope registry key not found: {CrawlScopeKey}"));
            }

            sb.AppendLine($"[{CrawlScopeKey}]");
            foreach (string subKeyName in key.GetSubKeyNames()) sb.AppendLine($"  {subKeyName}");

            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Search indexing scope listing failed: {ex.Message}"));
        }
    }








    [Description("Reads Windows Search service configuration from the registry.")]
    public Task<ToolResult> search_indexing_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(WindowsSearchKey, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Windows Search registry key not found: {WindowsSearchKey}"));
            }

            sb.AppendLine($"[{WindowsSearchKey}]");
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Search indexing settings read failed: {ex.Message}"));
        }
    }
}