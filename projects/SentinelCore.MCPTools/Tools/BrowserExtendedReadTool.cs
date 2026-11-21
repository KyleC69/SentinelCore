// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         BrowserExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tools for querying browser extensions
///     from Chrome and Edge local profiles.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class BrowserExtendedReadTool
{

    [McpServerTool(Name = "sc_forensics_list_browser_extensions", ReadOnly = true, Destructive = false)]
    [Description("Lists installed browser extensions for Chrome and Edge from their local profile directories.")]
    public async Task<ToolResult> BrowserListExtensionsAsync([Description("The browser to query: Chrome, Edge, or All. Defaults to All.")] string browser = "All", [Description("Maximum number of extensions to return per browser. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            List<object> results = new();

            if (browser.Equals("Chrome", StringComparison.OrdinalIgnoreCase) || browser.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                string chromePath = GetChromeProfilePath();
                if (Directory.Exists(chromePath))
                {
                    string extensionsPath = Path.Combine(chromePath, "Default", "Extensions");
                    if (Directory.Exists(extensionsPath))
                    {
                        foreach (string extDir in Directory.GetDirectories(extensionsPath))
                        {
                            if (results.Count >= maxRecords) break;
                            string extId = Path.GetFileName(extDir);
                            string[] versionDirs = Directory.GetDirectories(extDir);
                            string version = versionDirs.Length > 0 ? Path.GetFileName(versionDirs[0]) : "unknown";

                            results.Add(new { Browser = "Chrome", ExtensionId = extId, Version = version, Path = extDir });
                        }
                    }
                }
            }

            if (browser.Equals("Edge", StringComparison.OrdinalIgnoreCase) || browser.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                string edgePath = GetEdgeProfilePath();
                if (Directory.Exists(edgePath))
                {
                    string extensionsPath = Path.Combine(edgePath, "Default", "Extensions");
                    if (Directory.Exists(extensionsPath))
                    {
                        foreach (string extDir in Directory.GetDirectories(extensionsPath))
                        {
                            if (results.Count >= maxRecords) break;
                            string extId = Path.GetFileName(extDir);
                            string[] versionDirs = Directory.GetDirectories(extDir);
                            string version = versionDirs.Length > 0 ? Path.GetFileName(versionDirs[0]) : "unknown";

                            results.Add(new { Browser = "Edge", ExtensionId = extId, Version = version, Path = extDir });
                        }
                    }
                }
            }

            return ToolResult.Ok(results, "BrowserExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Browser extension listing");
        }
    }








    private string GetChromeProfilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
    }








    private string GetEdgeProfilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data");
    }
}
