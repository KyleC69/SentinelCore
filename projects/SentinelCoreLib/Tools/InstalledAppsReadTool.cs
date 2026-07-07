// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         InstalledAppsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Management;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Win32;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for enumerating installed applications via MSI registry entries and the Win32_Product CIM class.
/// </summary>
public sealed class InstalledAppsReadTool : AITool
{
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Wow64UninstallKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";








    private static void CollectFromRegistry(RegistryHive hive, string keyPath, List<Dictionary<string, string?>> results, string? filter)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? uninstallKey = baseKey.OpenSubKey(keyPath, writable: false);
        if (uninstallKey is null)
        {
            return;
        }

        foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            try
            {
                using RegistryKey? subKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                if (subKey is null)
                {
                    continue;
                }

                string? displayName = subKey.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(filter) && displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                results.Add(new()
                {
                        ["DisplayName"] = displayName,
                        ["Publisher"] = subKey.GetValue("Publisher")?.ToString(),
                        ["Version"] = subKey.GetValue("DisplayVersion")?.ToString(),
                        ["InstallDate"] = subKey.GetValue("InstallDate")?.ToString(),
                        ["UninstallString"] = subKey.GetValue("UninstallString")?.ToString(),
                        ["RegistryPath"] = $"{hive}\\{keyPath}\\{subKeyName}"
                });
            }
            catch
            {
                // Ignore individual corrupted entries.
            }
    }








    [Description("Lists installed applications from the Add/Remove Programs registry entries.")]
    public Task<ToolResult> installed_apps_list([Description("Optional publisher or display name filter (partial match).")] string? filter = null)
    {
        try
        {
            List<Dictionary<string, string?>> results = new();
            CollectFromRegistry(RegistryHive.LocalMachine, UninstallKey, results, filter);
            CollectFromRegistry(RegistryHive.LocalMachine, Wow64UninstallKey, results, filter);
            CollectFromRegistry(RegistryHive.CurrentUser, UninstallKey, results, filter);

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Installed app listing failed: {ex.Message}"));
        }
    }








    [Description("Lists installed applications using the Win32_Product CIM provider (MSI API surface).")]
    public Task<ToolResult> installed_apps_msi_list([Description("Optional product name filter (partial match).")] string? filter = null)
    {
        try
        {
            List<object> results = new();
            using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Name, Version, Vendor, InstallDate, IdentifyingNumber FROM Win32_Product");
            foreach (ManagementObject product in searcher.Get())
            {
                string name = product["Name"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                results.Add(new
                {
                        Name = name,
                        Version = product["Version"]?.ToString(),
                        Vendor = product["Vendor"]?.ToString(),
                        InstallDate = product["InstallDate"]?.ToString(),
                        IdentifyingNumber = product["IdentifyingNumber"]?.ToString()
                });
            }

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"MSI product listing failed: {ex.Message}"));
        }
    }
}