// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         NotificationsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.AI;
using Microsoft.Win32;

using System.ComponentModel;
using System.Text;
using System.Text.Json;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows notification platform configuration and installed notification apps.
/// </summary>
public sealed class NotificationsReadTool : AITool
{
    private const string ToastKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";








    [Description("Lists notification settings and app entries from the Windows notification registry store.")]
    public Task<ToolResult> notification_list_apps()
    {
        try
        {
            var results = new List<Dictionary<string, object?>>();
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? settingsKey = baseKey.OpenSubKey(ToastKey, writable: false);
            if (settingsKey is not null)
            {
                foreach (string appKeyName in settingsKey.GetSubKeyNames())
                    try
                    {
                        using RegistryKey? appKey = settingsKey.OpenSubKey(appKeyName, writable: false);
                        if (appKey is null)
                        {
                            continue;
                        }

                        results.Add(new()
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
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Notification app listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the global Windows quiet hours / do-not-disturb state from the registry.")]
    public Task<ToolResult> notification_read_quiet_hours()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\NOC_GLOBAL_SETTING", writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.SuccessResult("Quiet-hours registry key not present."));
            }

            StringBuilder sb = new();
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"{valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Quiet hours read failed: {ex.Message}"));
        }
    }
}