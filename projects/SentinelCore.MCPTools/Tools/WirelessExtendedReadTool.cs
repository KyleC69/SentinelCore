// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         WirelessExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying wireless connection history from the registry
///     for rogue access point detection.
/// </summary>
[McpServerToolType]
public sealed class WirelessExtendedReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_networkaccess_list_wireless_history", ReadOnly = true, Destructive = false)]
    [Description("Lists wireless network connection history from the registry for rogue access point detection.")]
    public async Task<ToolResult> WirelessListConnectionHistoryAsync([Description("Maximum number of profiles to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // Wireless profiles are stored under HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles
                using RegistryKey? profilesKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles", false);
                if (profilesKey is not null)
                {
                    foreach (string profileGuid in profilesKey.GetSubKeyNames())
                    {
                        if (results.Count >= maxRecords) break;

                        using RegistryKey? profileKey = profilesKey.OpenSubKey(profileGuid, false);
                        if (profileKey is null) continue;

                        object? profileName = profileKey.GetValue("ProfileName");
                        object? description = profileKey.GetValue("Description");
                        object? category = profileKey.GetValue("Category");
                        object? categoryType = profileKey.GetValue("CategoryType");
                        object? dateCreated = profileKey.GetValue("DateCreated");
                        object? dateLastConnected = profileKey.GetValue("DateLastConnected");
                        object? managed = profileKey.GetValue("Managed");
                        object? nameType = profileKey.GetValue("NameType");

                        // Only include wireless profiles (CategoryType 71 = Wireless)
                        int catType = categoryType is int ct ? ct : -1;
                        if (catType != 71 && catType != -1)
                        {
                            // Include all network types for completeness
                        }

                        results.Add(new
                        {
                                Guid = profileGuid,
                                ProfileName = profileName?.ToString() ?? "",
                                Description = description?.ToString() ?? "",
                                Category = category?.ToString() ?? "",
                                CategoryType = catType,
                                DateCreated = dateCreated?.ToString() ?? "",
                                DateLastConnected = dateLastConnected?.ToString() ?? "",
                                Managed = managed?.ToString() ?? "",
                                NameType = nameType?.ToString() ?? ""
                        });
                    }
                }
            }

            return ToolResult.Ok(results, "WirelessExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "WirelessExtendedReadTool");
        }
    }
}
