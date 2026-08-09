// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NotificationsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows notification platform configuration and installed notification apps.
/// </summary>
public sealed class NotificationsReadTool : AITool
{

    private const string ToastKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    public override string Description { get; } = "Read-only tool for querying Windows notification platform configuration and installed notification apps.";
    public override string Name { get; } = "Notifications_Read";








    [Description("Lists notification settings and app entries from the Windows notification registry store.")]
    public Task<ToolResult> notification_list_apps()
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

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Notification app listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the global Windows quiet hours / do-not-disturb state from the registry.")]
    public Task<ToolResult> notification_read_quiet_hours()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\NOC_GLOBAL_SETTING", false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Ok("Quiet-hours registry key not present."));
            }

            StringBuilder sb = new();
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"{valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Quiet hours read failed: {ex.Message}"));
        }
    }
}