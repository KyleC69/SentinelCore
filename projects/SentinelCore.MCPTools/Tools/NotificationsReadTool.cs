// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         NotificationsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows notification platform configuration and installed notification apps.
/// </summary>
[McpServerToolType]
public sealed class NotificationsReadTool
{

    private const string ToastKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_list_notification_apps", ReadOnly = true, Destructive = false)]
    [Description("Lists notification settings and app entries from the Windows notification registry store.")]
    public async Task<ToolResult> NotificationListAppsAsync()
    {
        try
        {
            List<Dictionary<string, object?>> results = new();
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? settingsKey = baseKey.OpenSubKey(ToastKey, false);
            if (settingsKey is not null)
            {
                foreach (string appKeyName in settingsKey.GetSubKeyNames())
                    try
                    {
                        using RegistryKey? appKey = settingsKey.OpenSubKey(appKeyName, false);
                        if (appKey is null)
                        {
                            continue;
                        }

                        results.Add(new Dictionary<string, object?>
                        {
                                ["App"] = appKeyName,
                                ["Enabled"] = appKey.GetValue("Enabled"),
                                ["ShowBanner"] = appKey.GetValue("ShowBannerAndSound"),
                                ["ShowNotificationActions"] = appKey.GetValue("ShowNotificationActions"),
                                ["LastModified"] = appKey.GetValue("LastNotificationAdded")
                        });
                    }
                    catch
                    {

                        // Ignore unreadable entries.

                    }
            }

            return ToolResult.Ok(results, "NotificationsReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "NotificationsReadTool");

        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_notification_settings", ReadOnly = true, Destructive = false)]
    [Description("Reads the global Windows quiet hours / do-not-disturb state from the registry.")]
    public async Task<ToolResult> NotificationReadQuietHoursAsync()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\NOC_GLOBAL_SETTING", false);
            if (key is null)
            {
                return ToolResult.Ok("Quiet hours not configured.", "NotificationsReadTool");
            }

            StringBuilder sb = new();
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"{valueName}={key.GetValue(valueName)}");

            return ToolResult.Ok(sb.ToString(), "NotificationsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "NotificationsReadTool");
        }
    }
}
