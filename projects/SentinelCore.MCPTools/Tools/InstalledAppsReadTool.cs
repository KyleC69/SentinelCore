// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         InstalledAppsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating installed applications from the Add/Remove Programs registry locations.
///     MSI-installed products are additionally exposed from their Windows Installer product registration data,
///     which mirrors the Win32_Product data surface without triggering an installer consistency check.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class InstalledAppsReadTool
{

    /// <summary>
    ///     The Add/Remove Programs registry path relative to the SOFTWARE hive root.
    /// </summary>
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <summary>
    ///     The Add/Remove Programs path expressed in the WOW64-redirected form used by the 32-bit registry view.
    /// </summary>
    private const string Wow64UninstallKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <summary>The value name holding the user-visible product name.</summary>
    private const string DisplayNameValueName = "DisplayName";

    /// <summary>The value name holding the publisher.</summary>
    private const string PublisherValueName = "Publisher";

    /// <summary>The value name holding the display version.</summary>
    private const string DisplayVersionValueName = "DisplayVersion";

    /// <summary>The value name holding the install date.</summary>
    private const string InstallDateValueName = "InstallDate";

    /// <summary>The value name holding the uninstall command line.</summary>
    private const string UninstallStringValueName = "UninstallString";

    /// <summary>The value name that flags a Windows Installer (MSI) product registration.</summary>
    private const string WindowsInstallerValueName = "WindowsInstaller";

    /// <summary>The default number of records returned by both tools.</summary>
    private const int DefaultMaxRecords = 50;

    /// <summary>
    ///     The registry locations that hold Add/Remove Programs entries. The 64-bit and the WOW64-redirected
    ///     32-bit views of the machine hive are both enumerated, together with the per-user hive, so that x86,
    ///     x64 and ARM64 installs are all reported.
    /// </summary>
    private static readonly (RegistryHive Hive, RegistryView View)[] UninstallLocations =
    [
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Default)
    ];








    /// <summary>
    ///     Runs the blocking registry enumeration on the thread pool so the MCP request pipeline is never blocked.
    /// </summary>
    /// <param name="filter">Optional case-insensitive partial filter applied to the display name or publisher.</param>
    /// <param name="cancellationToken">Token used to cancel the enumeration.</param>
    /// <returns>The ordered uninstall entries that match the filter.</returns>
    private static async Task<IReadOnlyList<UninstallEntry>> CollectAsync(string? filter, CancellationToken cancellationToken)
    {
        return await Task.Run(() => EnumerateUninstallEntries(NormalizeFilter(filter), cancellationToken), cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Enumerates the Add/Remove Programs entries from every configured registry location.
    ///     The 64-bit and 32-bit views expose disjoint key sets, so every registered entry is reported exactly
    ///     once. Entries are ordered by display name so that identical machine state always produces identical
    ///     output. Truncation is left to the caller so that caller-specific projections (for example the MSI-only
    ///     projection) trim the correct result set.
    /// </summary>
    /// <param name="filter">Optional case-insensitive partial filter applied to the display name or publisher.</param>
    /// <param name="cancellationToken">Token used to cancel the enumeration.</param>
    /// <returns>The ordered uninstall entries that match the filter.</returns>
    private static IReadOnlyList<UninstallEntry> EnumerateUninstallEntries(string? filter, CancellationToken cancellationToken)
    {
        List<UninstallEntry> entries = new();

        foreach ((RegistryHive hive, RegistryView view) in UninstallLocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? uninstallKey = baseKey.OpenSubKey(UninstallKeyPath, writable: false);
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                UninstallEntry? entry = TryReadEntry(uninstallKey, hive, view, subKeyName);
                if (entry is null || !MatchesFilter(entry, filter))
                {
                    continue;
                }

                entries.Add(entry);
            }
        }

        entries.Sort(CompareEntries);

        return entries;
    }








    /// <summary>
    ///     Reads a single Add/Remove Programs subkey into an <see cref="UninstallEntry" />.
    /// </summary>
    /// <param name="uninstallKey">The opened Uninstall key that contains the entry.</param>
    /// <param name="hive">The hive the entry was read from.</param>
    /// <param name="view">The registry view the entry was read through.</param>
    /// <param name="subKeyName">The name of the entry subkey.</param>
    /// <returns>The populated entry, or <c>null</c> when the entry has no display name or cannot be read.</returns>
    private static UninstallEntry? TryReadEntry(RegistryKey uninstallKey, RegistryHive hive, RegistryView view, string subKeyName)
    {
        try
        {
            using RegistryKey? subKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
            if (subKey is null)
            {
                return null;
            }

            string displayName = Helpers.CleanValue(subKey.GetValue(DisplayNameValueName)?.ToString());
            if (displayName.Length == 0)
            {
                // Entries without a display name are not user-visible applications.
                return null;
            }

            return new UninstallEntry(subKeyName, displayName, Helpers.CleanValue(subKey.GetValue(PublisherValueName)?.ToString()), Helpers.CleanValue(subKey.GetValue(DisplayVersionValueName)?.ToString()), Helpers.CleanValue(subKey.GetValue(InstallDateValueName)?.ToString()), Helpers.CleanValue(subKey.GetValue(UninstallStringValueName)?.ToString()), BuildRegistryPath(hive, view, subKeyName), IsMsiProduct(subKey, subKeyName));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            // Individual entries can be access-restricted or corrupted; skip the entry instead of failing the enumeration.
            return null;
        }
    }








    /// <summary>
    ///     Determines whether a subkey represents a Windows Installer (MSI) product registration.
    /// </summary>
    /// <param name="subKey">The opened entry subkey.</param>
    /// <param name="subKeyName">The name of the entry subkey.</param>
    /// <returns><c>true</c> when the entry is an MSI product registration; otherwise <c>false</c>.</returns>
    private static bool IsMsiProduct(RegistryKey subKey, string subKeyName)
    {
        object? marker = subKey.GetValue(WindowsInstallerValueName);
        if (marker is not null && int.TryParse(marker.ToString(), out int markerValue) && markerValue == 1)
        {
            return true;
        }

        // MSI product registrations always use the product code GUID as the key name.
        return subKeyName.StartsWith('{') && subKeyName.EndsWith('}');
    }








    /// <summary>
    ///     Tests an entry against the optional partial filter.
    /// </summary>
    /// <param name="entry">The entry to test.</param>
    /// <param name="filter">The normalized filter, or <c>null</c> when no filtering is requested.</param>
    /// <returns><c>true</c> when the entry matches the filter or no filter was supplied.</returns>
    private static bool MatchesFilter(UninstallEntry entry, string? filter)
    {
        if (filter is null)
        {
            return true;
        }

        return entry.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) || entry.Publisher.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }








    /// <summary>
    ///     Normalizes the optional caller supplied filter.
    /// </summary>
    /// <param name="filter">The raw filter value.</param>
    /// <returns>The trimmed filter, or <c>null</c> when no meaningful filter was supplied.</returns>
    private static string? NormalizeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        return filter.Trim();
    }








    /// <summary>
    ///     Builds the fully qualified registry path of an entry, honouring WOW64 redirection.
    /// </summary>
    /// <param name="hive">The hive the entry was read from.</param>
    /// <param name="view">The registry view the entry was read through.</param>
    /// <param name="subKeyName">The name of the entry subkey.</param>
    /// <returns>The fully qualified registry path of the entry.</returns>
    private static string BuildRegistryPath(RegistryHive hive, RegistryView view, string subKeyName)
    {
        string hiveName = hive switch
        {
                RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
                RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
                _ => hive.ToString()
        };

        string keyPath = view == RegistryView.Registry32 ? Wow64UninstallKeyPath : UninstallKeyPath;

        return $"{hiveName}\\{keyPath}\\{subKeyName}";
    }








    /// <summary>
    ///     Orders entries by display name, version and registry path so enumeration output is fully deterministic.
    ///     The registry path is unique per registration and acts as a total-order tie-breaker.
    /// </summary>
    /// <param name="left">The first entry to compare.</param>
    /// <param name="right">The second entry to compare.</param>
    /// <returns>A signed value indicating the relative order of the two entries.</returns>
    private static int CompareEntries(UninstallEntry left, UninstallEntry right)
    {
        int comparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.Compare(left.Version, right.Version, StringComparison.OrdinalIgnoreCase);

        return comparison != 0 ? comparison : string.CompareOrdinal(left.RegistryPath, right.RegistryPath);
    }








    /// <summary>
    ///     Lists the applications registered in the Add/Remove Programs locations.
    /// </summary>
    /// <param name="filter">Optional case-insensitive partial filter matched against the display name or publisher.</param>
    /// <param name="maxRecords">The maximum number of applications to return. Must be between 1 and 500.</param>
    /// <param name="cancellationToken">Token used to cancel the registry enumeration.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed application records, or a failure result.</returns>
    [McpServerTool(Name = "sc_configuration_list_installed_apps", ReadOnly = true, Destructive = false)]
    [Description("Lists installed applications from the Add/Remove Programs registry entries. The optional filter matches the display name or the publisher.")]
    public async Task<ToolResult> InstalledAppsListAsync([Description("Optional publisher or display name filter (partial, case-insensitive match).")] string? filter = null, [Description("Maximum number of applications to return. Must be between 1 and 500. Defaults to 50.")] int maxRecords = DefaultMaxRecords, CancellationToken cancellationToken = default)
    {
        ToolResult? validationResult = InputValidator.ValidateMaxRecords(maxRecords);
        if (validationResult is not null)
        {
            return validationResult;
        }

        try
        {
            IReadOnlyList<UninstallEntry> entries = await CollectAsync(filter, cancellationToken).ConfigureAwait(false);

            List<InstalledApplicationRecord> applications = entries.Take(maxRecords).Select(static entry => new InstalledApplicationRecord(entry.DisplayName, entry.Publisher, entry.Version, entry.InstallDate, entry.UninstallString, entry.RegistryPath)).ToList();

            return ToolResult.Ok(applications, $"InstalledAppsReadTool returned {applications.Count} registered application(s).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail("The operation was cancelled before completion.", "InstalledAppsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "InstalledAppsReadTool");
        }
    }








    /// <summary>
    ///     Lists the Windows Installer (MSI) products registered on the machine.
    ///     The data is read from the product registration entries rather than the Win32_Product class, because
    ///     querying Win32_Product forces Windows Installer to reconfigure every installed product.
    /// </summary>
    /// <param name="filter">Optional case-insensitive partial filter matched against the product name or vendor.</param>
    /// <param name="maxRecords">The maximum number of products to return. Must be between 1 and 500.</param>
    /// <param name="cancellationToken">Token used to cancel the registry enumeration.</param>
    /// <returns>A <see cref="ToolResult" /> containing the typed MSI product records, or a failure result.</returns>
    [McpServerTool(Name = "sc_configuration_list_msi_products", ReadOnly = true, Destructive = false)]
    [Description("Lists Windows Installer (MSI) installed products from their product registration entries. Equivalent to the Win32_Product data without triggering an installer consistency check.")]
    public async Task<ToolResult> InstalledAppsMsiListAsync([Description("Optional product name or vendor filter (partial, case-insensitive match).")] string? filter = null, [Description("Maximum number of products to return. Must be between 1 and 500. Defaults to 50.")] int maxRecords = DefaultMaxRecords, CancellationToken cancellationToken = default)
    {
        ToolResult? validationResult = InputValidator.ValidateMaxRecords(maxRecords);
        if (validationResult is not null)
        {
            return validationResult;
        }

        try
        {
            IReadOnlyList<UninstallEntry> entries = await CollectAsync(filter, cancellationToken).ConfigureAwait(false);

            List<MsiProductRecord> products = entries.Where(static entry => entry.IsMsiProduct).Take(maxRecords).Select(static entry => new MsiProductRecord(entry.DisplayName, entry.Version, entry.Publisher, entry.InstallDate, entry.SubKeyName)).ToList();

            return ToolResult.Ok(products, $"InstalledAppsReadTool returned {products.Count} registered MSI product(s).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail("The operation was cancelled before completion.", "InstalledAppsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "InstalledAppsReadTool");
        }
    }








    /// <summary>
    ///     A raw uninstall registry entry shared by the application and the MSI enumeration paths.
    /// </summary>
    /// <param name="SubKeyName">The registry subkey name; for MSI products this is the product code GUID.</param>
    /// <param name="DisplayName">The user-visible application name.</param>
    /// <param name="Publisher">The registered publisher.</param>
    /// <param name="Version">The registered display version.</param>
    /// <param name="InstallDate">The registered install date.</param>
    /// <param name="UninstallString">The registered uninstall command line.</param>
    /// <param name="RegistryPath">The fully qualified registry path of the entry.</param>
    /// <param name="IsMsiProduct">Whether the entry is a Windows Installer product registration.</param>
    private sealed record UninstallEntry(string SubKeyName, string DisplayName, string Publisher, string Version, string InstallDate, string UninstallString, string RegistryPath, bool IsMsiProduct);





    /// <summary>
    ///     An application registered under the Add/Remove Programs registry locations.
    /// </summary>
    /// <param name="DisplayName">The user-visible application name.</param>
    /// <param name="Publisher">The publisher, or an empty string when it is not registered.</param>
    /// <param name="Version">The display version, or an empty string when it is not registered.</param>
    /// <param name="InstallDate">The install date in the registered format (yyyyMMdd), or an empty string when it is not registered.</param>
    /// <param name="UninstallString">The registered uninstall command line, or an empty string when it is not registered.</param>
    /// <param name="RegistryPath">The fully qualified registry path of the entry.</param>
    public sealed record InstalledApplicationRecord(string DisplayName, string Publisher, string Version, string InstallDate, string UninstallString, string RegistryPath);





    /// <summary>
    ///     A registered Windows Installer (MSI) product.
    /// </summary>
    /// <param name="Name">The product display name.</param>
    /// <param name="Version">The product version.</param>
    /// <param name="Vendor">The product vendor (publisher).</param>
    /// <param name="InstallDate">The install date in the registered format (yyyyMMdd).</param>
    /// <param name="IdentifyingNumber">The product code GUID that identifies the MSI product.</param>
    public sealed record MsiProductRecord(string Name, string Version, string Vendor, string InstallDate, string IdentifyingNumber);
}
