// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         SearchIndexingReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Search indexing options.
/// </summary>
[McpServerToolType]
public sealed class SearchIndexingReadTool
{

    private const string CrawlScopeKey = "SOFTWARE\\Microsoft\\Windows Search\\CrawlScopeManager";
    private const string WindowsSearchKey = "SOFTWARE\\Microsoft\\Windows Search";








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_list_search_scopes", ReadOnly = true, Destructive = false)]
    [Description("Lists indexed locations from the Windows Search crawl scope registry.")]
    public async Task<ToolResult> SearchIndexingListScopesAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CrawlScopeKey, false);
            if (key is null)
            {
                return ToolResult.Fail($"Crawl scope registry key not found: {CrawlScopeKey}", "SearchIndexingReadTool");
            }

            foreach (string subKeyName in key.GetSubKeyNames())
            {
                results.Add(new { Type = "SubKey", Name = subKeyName });
            }

            foreach (string valueName in key.GetValueNames())
            {
                results.Add(new { Type = "Value", Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
            }

            return ToolResult.Ok(results, "SearchIndexingReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "SearchIndexingReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_search_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads Windows Search service configuration from the registry.")]
    public async Task<ToolResult> SearchIndexingReadSettingsAsync()
    {
        try
        {
            List<object> results = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(WindowsSearchKey, false);
            if (key is null)
            {
                return ToolResult.Fail($"Windows Search registry key not found: {WindowsSearchKey}", "SearchIndexingReadTool");
            }

            foreach (string valueName in key.GetValueNames())
            {
                results.Add(new { Name = valueName, Value = Helpers.CleanValue(key.GetValue(valueName)?.ToString()) });
            }

            return ToolResult.Ok(results, "SearchIndexingReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "SearchIndexingReadTool");
        }
    }
}
