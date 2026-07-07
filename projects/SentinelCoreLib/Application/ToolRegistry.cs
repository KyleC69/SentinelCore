// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ToolRegistry.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.AI;

using SentinelCoreLib.Tools;




namespace SentinelCoreLib.Application;





/// <summary>
///     tool registry is not a registry in the traditional sense, there are no DI registrations, no service lifetimes, and
///     no dependency injection of tools to be used by domain agents. They are to beinstantiated by the domain agent and
///     used as needed. The registry is a collection of tools that are available to the domain agents. The registry is used
///     to retrieve tools by name or by domain. The registry is also used to check if a tool is registered(exists as tool).
///     The registry is not responsible for the lifecycle of the tools, it is only responsible for providing access to the
///     tools.
/// </summary>
/// <remarks>
///     This class provides the methods needed to retrieve tools by their names or associated domains.
///     It supports case-insensitive name matching and provides methods to check
///     for tool existence, retrieve tools by name, and retrieve tools by domain.
/// </remarks>
public static class ToolRegistry
{
    private static readonly Dictionary<string, IReadOnlyList<string>> _domainToolNames;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolRegistry" /> class.
    /// </summary>
    /// <param name="tools">The tools to register.</param>
    static ToolRegistry()
    {



        _domainToolNames = new(StringComparer.OrdinalIgnoreCase)
        {
                ["registry"] = [nameof(RegistryReadTool)],
                ["dcom"] = [nameof(DcomReadTool)],
                ["wmi"] = [nameof(WmiQueryTool)],
                ["filesystem"] = [nameof(FileSystemReadTool)],
                ["grouppolicy"] = [nameof(GroupPolicyReadTool)],
                ["services"] = [nameof(WindowsServiceReadTool)],
                ["scheduledtasks"] = [nameof(ScheduledTaskReadTool)],
                ["network"] = [nameof(NetworkReadTool)],
                ["firewall"] = [nameof(FirewallReadTool)],
                ["power"] = [nameof(PowerSettingsReadTool)],
                ["localaccounts"] = [nameof(LocalAccountsReadTool)],
                ["eventlog"] = [nameof(EventLogReadTool)],
                ["applocker"] = [nameof(AppLockerReadTool)],
                ["windowsupdate"] = [nameof(WindowsUpdateReadTool)],
                ["pnpdevices"] = [nameof(PnpDeviceReadTool)],
                ["environment"] = [nameof(EnvironmentVariablesReadTool)],
                ["shellexplorer"] = [nameof(ShellExplorerReadTool)],
                ["certificates"] = [nameof(CertificateStoreReadTool)],
                ["hyperv"] = [nameof(HyperVReadTool)],
                ["rdp"] = [nameof(RemoteDesktopReadTool)],
                ["bootconfig"] = [nameof(BootConfigurationReadTool)],
                ["accessibility"] = [nameof(AccessibilityReadTool)],
                ["searchindexing"] = [nameof(SearchIndexingReadTool)],
                ["audio"] = [nameof(AudioDeviceReadTool)],
                ["printers"] = [nameof(PrinterReadTool)],
                ["drivers"] = [nameof(DriversReadTool)],
                ["processes"] = [nameof(ProcessesReadTool)],
                ["performance"] = [nameof(PerformanceReadTool)],
                ["installedapps"] = [nameof(InstalledAppsReadTool)],
                ["browserconfig"] = [nameof(BrowserConfigReadTool)],
                ["fonts"] = [nameof(FontsReadTool)],
                ["notifications"] = [nameof(NotificationsReadTool)],
                ["vpn"] = [nameof(VpnReadTool)],
                ["wireless"] = [nameof(WirelessReadTool)],
                ["proxy"] = [nameof(ProxyReadTool)],
                ["sensors"] = [nameof(SensorsReadTool)],
                ["battery"] = [nameof(BatteryReadTool)],
                ["display"] = [nameof(DisplayReadTool)],
                ["credentials"] = [nameof(CredentialsReadTool)],
                ["uac"] = [nameof(UacReadTool)],
                ["defender"] = [nameof(DefenderReadTool)],
                ["bitlocker"] = [nameof(BitlockerReadTool)]
        };
    }








    /// <summary>
    ///     Gets a read-only dictionary of domain names and their associated tool names.
    ///     You then use the domain name to retrieve the tools associated with that domain using the GetToolByDomain method.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DomainNames
    {
        get => _domainToolNames;
    }








    /// <summary>
    ///     Determines whether a tool with the specified name exists in the registry.
    /// </summary>
    /// <param name="name">The name of the tool to check for existence.</param>
    /// <returns>
    ///     <c>true</c> if a tool with the specified name exists in the registry; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the <paramref name="name" /> parameter is <c>null</c>, empty, or consists only of white-space characters.
    /// </exception>
    public static bool Contains(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _domainToolNames.TryGetValue(name, out var toolNames);
        return toolNames is not null && toolNames.Count > 0;
    }








    private static AITool? CreateToolFromDomain(string domain)
    {
        return domain.ToLowerInvariant() switch
        {
                "registry" => new RegistryReadTool(),
                "dcom" => new DcomReadTool(),
                "wmi" => new WmiQueryTool(),
                "filesystem" => new FileSystemReadTool(),
                "grouppolicy" => new GroupPolicyReadTool(),
                "services" => new WindowsServiceReadTool(),
                "scheduledtasks" => new ScheduledTaskReadTool(),
                "network" => new NetworkReadTool(),
                "firewall" => new FirewallReadTool(),
                "power" => new PowerSettingsReadTool(),
                "localaccounts" => new LocalAccountsReadTool(),
                "eventlog" => new EventLogReadTool(),
                "applocker" => new AppLockerReadTool(),
                "windowsupdate" => new WindowsUpdateReadTool(),
                "pnpdevices" => new PnpDeviceReadTool(),
                "environment" => new EnvironmentVariablesReadTool(),
                "shellexplorer" => new ShellExplorerReadTool(),
                "certificates" => new CertificateStoreReadTool(),
                "hyperv" => new HyperVReadTool(),
                "rdp" => new RemoteDesktopReadTool(),
                "bootconfig" => new BootConfigurationReadTool(),
                "accessibility" => new AccessibilityReadTool(),
                "searchindexing" => new SearchIndexingReadTool(),
                "audio" => new AudioDeviceReadTool(),
                "printers" => new PrinterReadTool(),
                "drivers" => new DriversReadTool(),
                "processes" => new ProcessesReadTool(),
                "performance" => new PerformanceReadTool(),
                "installedapps" => new InstalledAppsReadTool(),
                "browserconfig" => new BrowserConfigReadTool(),
                "fonts" => new FontsReadTool(),
                "notifications" => new NotificationsReadTool(),
                "vpn" => new VpnReadTool(),
                "wireless" => new WirelessReadTool(),
                "proxy" => new ProxyReadTool(),
                "sensors" => new SensorsReadTool(),
                "battery" => new BatteryReadTool(),
                "display" => new DisplayReadTool(),
                "credentials" => new CredentialsReadTool(),
                "uac" => new UacReadTool(),
                "defender" => new DefenderReadTool(),
                "bitlocker" => new BitlockerReadTool(),
                _ => null
        };
    }








    private static AITool? CreateToolInstance(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalized = name.ToLowerInvariant();

        // First treat the input as a domain name.
        if (_domainToolNames.ContainsKey(normalized))
        {
            return CreateToolFromDomain(normalized);
        }

        // Fall back: the input may be a tool name. Find the domain that owns it.
        string? domain = _domainToolNames.Where(pair => pair.Value.Any(tool => string.Equals(tool, name, StringComparison.OrdinalIgnoreCase))).Select(pair => pair.Key).FirstOrDefault();

        return domain is not null ? CreateToolFromDomain(domain) : null;

    }








    private static AITool? GetTool(string name)
    {
        return CreateToolInstance(name);
    }








    /// <summary>
    ///     Retrieves a list of tools associated with the specified domain.
    ///     ***** This method is the only method to be used to retrieve tools. *****
    ///     Tools have been designed to be directly tied to the specific agent and the domain (target config surface) that the
    ///     agent is responsible for. The tools are not designed to be used by other agents or for other domains. The tools are
    ///     not designed to be used in a generic way. The tools are designed to be used by the specific agent that will use it
    ///     and the domain (target config surface) that the agent is responsible for. The tools are not designed to be used by
    ///     other agents or for other domains. The tools are not designed to be used in a generic way. The tools are designed
    ///     to be used by the specific agent that will use it and the domain (target config surface) that the agent is
    ///     responsible for.
    ///     The tools are not designed to be used by other agents or for other domains. The tools are not designed to be used
    ///     in a generic way.
    /// </summary>
    /// <param name="domain">The domain for which tools are to be retrieved.</param>
    /// <returns>
    ///     A list of tools associated with the specified domain, or <c>null</c> if no tools are found.
    ///     There may be multiple tools associated with a single domain, and this method will return all of them.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IList<AITool>? GetToolByDomain(string domain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        if (!_domainToolNames.TryGetValue(domain, out var toolNames))
        {
            return null;
        }

        return toolNames.Select(name => GetTool(name)).Where(tool => tool is not null).Cast<AITool>().ToList();
    }
}