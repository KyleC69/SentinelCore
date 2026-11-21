// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         WindowsUpdateExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying installed hotfixes and patch compliance status
///     via the Win32_QuickFixEngineering WMI class.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class WindowsUpdateExtendedReadTool
{

    /// <summary>
    ///     Lists installed Windows hotfixes (patch history) via the Win32_QuickFixEngineering WMI class.
    /// </summary>
    /// <param name="maxRecords">Maximum number of updates to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing JSON-formatted hotfix information.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_list_updates", ReadOnly = true, Destructive = false)]
    [Description("Lists installed Windows hotfixes (patch history) via the Win32_QuickFixEngineering WMI class for patch compliance analysis.")]
    public async Task<ToolResult> WindowsUpdateListAsync([Description("Maximum number of updates to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT HotFixID, Description, InstalledOn, InstalledBy FROM Win32_QuickFixEngineering");
            foreach (ManagementObject hotfix in searcher.Get())
            {
                if (results.Count >= maxRecords)
                {
                    break;
                }

                results.Add(new { HotFixID = hotfix["HotFixID"]?.ToString() ?? string.Empty, Description = hotfix["Description"]?.ToString() ?? string.Empty, InstalledOn = hotfix["InstalledOn"]?.ToString() ?? string.Empty, InstalledBy = hotfix["InstalledBy"]?.ToString() ?? string.Empty });
            }

            return ToolResult.Ok(results, "WindowsUpdateExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Hotfix listing");
        }
    }
}
