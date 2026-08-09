// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ToolRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Agents;
using SentinelCore.Tools;




namespace SentinelCore.Application;





/// <summary>
///     tool registry is not a registry in the traditional sense, there are no DI registrations, no service lifetimes, and
///     no dependency injection of tools to be used by domain agents. They are to be instantiated by the domain agent and
///     used as needed. The registry is a collection of toolsets, a domain agent is assigned a subset of which are
///     available to the domain agents.
///     This registry is used to retrieve tools by the target domain they interact with.
///     The registry is not responsible for the lifecycle of the tools, it is only responsible for providing access to the
///     tools.
/// </summary>
/// <remarks>
///     This class provides the methods needed to retrieve tools by their names or associated domains.
///     It supports case-insensitive name matching and provides methods to check
///     for tool existence, retrieve tools by domain.
/// </remarks>
public static class ToolRegistry
{
    private static readonly Dictionary<string, IList<string>> DomainToolNames;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolRegistry" /> class.
    /// </summary>
    static ToolRegistry()
    {
        DomainToolNames = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase)
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
                ["bitlocker"] = [nameof(BitlockerReadTool)],
                ["auditing"] = [nameof(AuditingTool)]
        };

        DomainNames = [.. DomainToolNames.Keys];
    }








    /// <summary>
    ///     A specialty list of the domains
    ///     You then use the domain name to retrieve the tools associated with that domain using the GetToolByDomain method.
    /// </summary>
    public static IList<string> DomainNames { get; set; }

    public static Dictionary<string, string[]> Toolsets { get; set; } = new(StringComparer.OrdinalIgnoreCase) { ["core"] = new[] { "registry", "dcom", "wmi", "filesystem", "grouppolicy", "services", "scheduledtasks", "network", "firewall", "power" }, ["set2"] = new[] { "localaccounts", "eventlog", "applocker", "windowsupdate", "pnpdevices", "environment", "shellexplorer", "certificates", "hyperv" }, ["set3"] = new[] { "rdp", "bootconfig", "accessibility", "searchindexing", "audio", "printers", "drivers", "processes", "performance", "installedapps" }, ["set4"] = new[] { "browserconfig", "fonts", "notifications", "vpn", "wireless", "proxy", "sensors", "battery", "display", "credentials", "uac", "defender", "bitlocker", "auditing" } };








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
        DomainToolNames.TryGetValue(name, out IList<string>? toolNames);
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
                "auditing" => new AuditingTool(),
                _ => null
        };
    }








    /// <summary>
    ///     Creates an instance of an AI tool based on the provided name.
    /// </summary>
    /// <param name="name">
    ///     The name of the tool or domain. This parameter is case-insensitive and must not be null, empty, or whitespace.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="AITool" /> if the tool or domain is found; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the <paramref name="name" /> is null, empty, or consists only of whitespace.
    /// </exception>
    private static AITool? CreateToolInstance(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalized = name.ToLowerInvariant();

        // First treat the input as a domain name.
        if (DomainToolNames.ContainsKey(normalized))
        {
            return CreateToolFromDomain(normalized);
        }

        // Fall back: the input may be a tool name. Find the domain that owns it.
        string? domain = DomainToolNames.Where(pair => pair.Value.Any(tool => string.Equals(tool, name, StringComparison.OrdinalIgnoreCase))).Select(pair => pair.Key).FirstOrDefault();

        return domain is not null ? CreateToolFromDomain(domain) : null;
    }








    /// <summary>
    ///     Returns instances of all tools registered in the registry.
    ///     Each domain in <see cref="DomainToolNames" /> is resolved to its corresponding
    ///     <see cref="AITool" /> implementation via <see cref="CreateToolFromDomain" />.
    /// </summary>
    /// <returns>A list containing one tool instance for every registered domain.</returns>
    public static IList<AITool> GetAllTools()
    {
        List<AITool> allTools = new(DomainToolNames.Count);

        foreach (string domain in DomainToolNames.Keys)
        {
            AITool? tool = CreateToolInstance(domain);

            if (tool is not null)
            {
                allTools.Add(tool);
            }
        }

        return allTools;
    }








    private static AITool? GetTool(string name)
    {
        return CreateToolInstance(name);
    }








    /// <summary>
    ///     Retrieves a list of tools associated with the specified domain. The domain is case-insensitive.
    ///     A list of domains can be retrieved via the <see cref="DomainNames" /> property. The tools are designed to be used
    ///     by domain agents for their specific domains.
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

        AITool? tool = CreateToolFromDomain(domain);
        return tool is not null ? [tool] : null;
    }








    /// <summary>
    ///     Resolves a list of tool or domain names into concrete <see cref="AITool" /> instances.
    ///     Used by dynamic (composite) agents that need tools spanning multiple domains.
    ///     Each name is resolved via <see cref="CreateToolInstance" /> which accepts both
    ///     domain names (e.g. "registry") and tool names (e.g. "RegistryReadTool").
    /// </summary>
    /// <param name="toolNames">The tool or domain names to resolve.</param>
    /// <returns>A list of resolved tools. May be empty if no names matched.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="toolNames" /> is null.</exception>
    public static IReadOnlyList<AITool> GetToolsByNames(IReadOnlyList<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        List<AITool> tools = [];
        foreach (string name in toolNames)
        {
            AITool? tool = GetTool(name);
            if (tool is not null)
            {
                tools.Add(tool);
            }
        }

        return tools;
    }








    public static IList<AITool>? GetToolsetByName(string setName)
    {
        switch (Toolsets.Keys.ToString())
        {
            case "set1":
                return GetToolsetByNames(Toolsets["set1"]);
            case "set2":
                return GetToolsetByNames(Toolsets["set2"]);
            case "set3":
                return GetToolsetByNames(Toolsets["set3"]);
            case "set4":
                return GetToolsetByNames(Toolsets["set4"]);
            default:
                return null;
        }
    }








    //Load the toolset by name and return the list of tools. If the toolset name is not found, return null.
    private static IList<AITool>? GetToolsetByNames(string[] strings)
    {
        throw new NotImplementedException();
    }








    /// <summary>
    ///     Returns the default toolset for the specified <see cref="AgentRole" />.
    ///     <para>
    ///         Role-based tool assignments:
    ///         <list type="bullet">
    ///             <item>
    ///                 <c>Core</c> — returns an empty list. Core's MCP research tools require a runtime endpoint URL
    ///                 and are added by <c>CoreAgentFactory</c> after the base spec is built.
    ///             </item>
    ///             <item><c>Manager</c> — empty. The manager delegates; it has no tools.</item>
    ///             <item>
    ///                 <c>Domain</c> — empty. Domain tools are resolved per-domain via
    ///                 <see cref="GetToolByDomain" /> and set by <c>DomainAgentFactory</c>.
    ///             </item>
    ///             <item>
    ///                 <c>Worker</c>, <c>General</c>, <c>Aggregator</c> — empty for now;
    ///                 toolsets will be assigned as workflows are defined.
    ///             </item>
    ///         </list>
    ///     </para>
    /// </summary>
    /// <param name="role">The agent role whose default toolset is requested.</param>
    /// <returns>
    ///     A read-only list of tools for the role. Currently, all roles return an empty list;
    ///     role-specific tools that require runtime parameters are added by the respective factory.
    /// </returns>
    internal static IList<AITool> GetToolsetByRole(AgentRole role)
    {
        // Core's MCP tools require a runtime endpoint URL, so they are added by CoreAgentFactory
        // after the base spec is built via AgentSpecBuilder. Domain tools are per-domain and
        // resolved by DomainAgentFactory. All other roles currently have no default toolset.
        return role switch
        {
                AgentRole.Core => [],
                AgentRole.Manager => [],
                AgentRole.Utility => GetAllTools(),
                _ => []
        };
    }
}