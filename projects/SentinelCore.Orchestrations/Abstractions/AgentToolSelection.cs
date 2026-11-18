// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentToolSelection.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Identifies how an <see cref="AgentToolSelection" /> chooses the tools bound to an agent.
/// </summary>
public enum AgentToolSelectionKind
{
    /// <summary>
    ///     No tools are bound to the agent.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Every tool in the merged MCP-server and local tool catalog is bound to the agent.
    /// </summary>
    All = 1,

    /// <summary>
    ///     Only the explicitly named tools are bound to the agent.
    /// </summary>
    Named = 2,

    /// <summary>
    ///     Only the tools claimed by one or more <see cref="AgentToolDomain" /> instances are bound to the agent.
    ///     Membership is computed from the live catalogue, so a tool added later is picked up automatically.
    /// </summary>
    Domains = 3
}





/// <summary>
///     Identifies the strategy a <see cref="ToolMatchRule" /> uses to decide whether a tool belongs to a domain.
/// </summary>
public enum ToolMatchKind
{
    /// <summary>
    ///     The tool name starts with the rule value. Used for area-owned names such as <c>Registry_</c>.
    ///     A trailing underscore is appended automatically when it is missing, so <c>Power</c> can never
    ///     claim <c>PowerShell_...</c>.
    /// </summary>
    Prefix = 0,

    /// <summary>
    ///     The tool name contains the rule value as a complete underscore-delimited segment. Used for
    ///     capability- or suite-owned names such as <c>PsList</c>, which must match both
    ///     <c>Sysinternals_PsList_List_Processes</c> and <c>PsList_List_Processes</c>.
    /// </summary>
    Token = 1
}





/// <summary>
///     Declares one membership test that contributes tools to an <see cref="AgentToolDomain" />.
///     Rules match names, never a fixed list, so a tool that is added or renamed is classified by
///     convention instead of being silently dropped.
/// </summary>
public sealed record ToolMatchRule
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolMatchRule" /> class.
    /// </summary>
    /// <param name="kind">The matching strategy.</param>
    /// <param name="value">The already-normalized value the strategy tests against.</param>
    private ToolMatchRule(ToolMatchKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }





    /// <summary>
    ///     Gets the matching strategy.
    /// </summary>
    public ToolMatchKind Kind { get; }





    /// <summary>
    ///     Gets the normalized value the strategy tests against.
    /// </summary>
    public string Value { get; }





    /// <summary>
    ///     Creates a rule that claims every tool whose name starts with the supplied area prefix.
    /// </summary>
    /// <param name="prefix">The area prefix, with or without a trailing underscore.</param>
    /// <returns>The prefix rule.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix" /> is null, empty, or whitespace.</exception>
    public static ToolMatchRule Prefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        string trimmed = prefix.Trim();

        // The separator is part of the contract: without it "Power" would claim every PowerShell tool.
        return new ToolMatchRule(ToolMatchKind.Prefix, trimmed.EndsWith('_') ? trimmed : trimmed + "_");
    }





    /// <summary>
    ///     Creates a rule that claims every tool whose name contains the supplied token as a whole segment.
    /// </summary>
    /// <param name="token">The capability or suite token, without separators.</param>
    /// <returns>The token rule.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token" /> is null, empty, or whitespace.</exception>
    public static ToolMatchRule Token(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new ToolMatchRule(ToolMatchKind.Token, token.Trim());
    }





    /// <summary>
    ///     Determines whether the supplied tool name satisfies this rule.
    /// </summary>
    /// <param name="toolName">The tool name to test.</param>
    /// <returns><c>true</c> when the rule claims the tool; otherwise <c>false</c>.</returns>
    public bool Matches(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        return Kind switch
        {
            ToolMatchKind.Prefix => toolName.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
            ToolMatchKind.Token => ContainsSegment(toolName, Value),
            _ => false
        };
    }





    /// <summary>
    ///     Returns a diagnostic representation of this rule.
    /// </summary>
    /// <returns>The strategy followed by the normalized value.</returns>
    public override string ToString()
    {
        return $"{Kind}({Value})";
    }





    /// <summary>
    ///     Determines whether an underscore-delimited segment of the tool name equals the supplied token.
    /// </summary>
    /// <param name="toolName">The tool name to split.</param>
    /// <param name="token">The token to find.</param>
    /// <returns><c>true</c> when a whole segment matches; otherwise <c>false</c>.</returns>
    private static bool ContainsSegment(string toolName, string token)
    {
        int start = 0;

        while (start <= toolName.Length)
        {
            int separator = toolName.IndexOf('_', start);

            ReadOnlySpan<char> segment = separator < 0
                    ? toolName.AsSpan(start)
                    : toolName.AsSpan(start, separator - start);

            if (segment.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (separator < 0)
            {
                return false;
            }

            start = separator + 1;
        }

        return false;
    }
}





/// <summary>
///     Names an area of the operating system or platform and the rules that decide which tools belong to it.
/// </summary>
/// <remarks>
///     <para>
///         A domain holds <em>rules</em>, never a fixed list of tool names. Membership is computed against the
///         live catalogue every time it is asked for, which is what makes tool loss structurally impossible:
///         a newly added tool is claimed by its area prefix on the next resolution.
///     </para>
///     <para>
///         Domains may overlap. <c>Registry_Read_Acl</c> legitimately belongs to both <see cref="AgentToolDomainCatalog.Registry" />
///         and <see cref="AgentToolDomainCatalog.Permissions" />; the factory de-duplicates by tool name.
///     </para>
/// </remarks>
public sealed record AgentToolDomain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentToolDomain" /> class.
    /// </summary>
    /// <param name="name">The stable domain name used at call sites and in diagnostics.</param>
    /// <param name="description">A short human-readable description of the domain.</param>
    /// <param name="rules">The membership rules. A domain with no rules claims no tools and is legal.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> or <paramref name="description" /> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <c>null</c>.</exception>
    public AgentToolDomain(string name, string description, params ToolMatchRule[] rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(rules);

        Name = name;
        Description = description;
        Rules = [.. rules];
    }





    /// <summary>
    ///     Gets the stable domain name used at call sites and in diagnostics.
    /// </summary>
    public string Name { get; }





    /// <summary>
    ///     Gets a short human-readable description of the domain.
    /// </summary>
    public string Description { get; }





    /// <summary>
    ///     Gets the membership rules that decide which tools belong to this domain.
    /// </summary>
    public IReadOnlyList<ToolMatchRule> Rules { get; }





    /// <summary>
    ///     Determines whether the supplied tool name belongs to this domain.
    /// </summary>
    /// <param name="toolName">The tool name to test.</param>
    /// <returns><c>true</c> when any rule claims the tool; otherwise <c>false</c>.</returns>
    public bool Matches(string toolName)
    {
        return Rules.Any(rule => rule.Matches(toolName));
    }





    /// <summary>
    ///     Selects the names from the supplied catalogue that belong to this domain.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to filter.</param>
    /// <returns>The matching tool names in catalogue order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    public IReadOnlyList<string> SelectFrom(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        return [.. toolNames.Where(Matches)];
    }





    /// <summary>
    ///     Returns a diagnostic representation of this domain.
    /// </summary>
    /// <returns>The domain name.</returns>
    public override string ToString()
    {
        return Name;
    }
}





/// <summary>
///     Describes the toolset that <see cref="ISentinelAgentFactory" /> binds to an agent at construction time.
///     The toolset may contain MCP-server tools, local in-process tools, or both.
/// </summary>
/// <remarks>
///     <para>
///         Tool binding must happen during agent construction because
///         <c>ChatClientAgentRunOptions.ChatOptions</c> unions its tool collection with the agent-level
///         tool collection. Passing tools per invocation therefore adds tools; it can never remove them.
///     </para>
///     <para>
///         Use <see cref="None" /> to bind no tools, <see cref="All" /> to bind the whole catalog, and
///         <see cref="FromNames" /> to bind a specific subset by name.
///     </para>
/// </remarks>
public sealed record AgentToolSelection
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentToolSelection" /> class.
    /// </summary>
    /// <param name="kind">The selection strategy.</param>
    /// <param name="toolNames">The explicitly named tools; empty for every kind except <see cref="AgentToolSelectionKind.Named" />.</param>
    /// <param name="domains">The requested domains; empty for every kind except <see cref="AgentToolSelectionKind.Domains" />.</param>
    private AgentToolSelection(AgentToolSelectionKind kind, IReadOnlyList<string> toolNames, IReadOnlyList<AgentToolDomain> domains)
    {
        Kind = kind;
        ToolNames = toolNames;
        Domains = domains;
    }





    /// <summary>
    ///     Gets the strategy that determines how the toolset is resolved.
    /// </summary>
    public AgentToolSelectionKind Kind { get; }





    /// <summary>
    ///     Gets the explicitly named tools requested by the call site. Empty unless
    ///     <see cref="Kind" /> is <see cref="AgentToolSelectionKind.Named" />.
    /// </summary>
    public IReadOnlyList<string> ToolNames { get; }





    /// <summary>
    ///     Gets the domains requested by the call site. Empty unless <see cref="Kind" /> is
    ///     <see cref="AgentToolSelectionKind.Domains" />.
    /// </summary>
    public IReadOnlyList<AgentToolDomain> Domains { get; }





    /// <summary>
    ///     Gets a selection that binds no tools to the agent.
    /// </summary>
    public static AgentToolSelection None { get; } = new(AgentToolSelectionKind.None, [], []);





    /// <summary>
    ///     Gets a selection that binds every tool in the merged MCP-server and local tool catalog.
    ///     This is the unfiltered catch-all used for debugging; it deliberately applies no domain rules.
    /// </summary>
    public static AgentToolSelection All { get; } = new(AgentToolSelectionKind.All, [], []);





    /// <summary>
    ///     Creates a selection that binds only the named tools.
    /// </summary>
    /// <param name="toolNames">The case-insensitive tool names to bind. Duplicates are collapsed.</param>
    /// <returns>A selection that binds the named tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="toolNames" /> is empty or contains a null, empty, or whitespace name.
    /// </exception>
    public static AgentToolSelection FromNames(params string[] toolNames)
    {
        return FromNames((IEnumerable<string>)toolNames);
    }





    /// <summary>
    ///     Creates a selection that binds only the named tools.
    /// </summary>
    /// <param name="toolNames">The case-insensitive tool names to bind. Duplicates are collapsed.</param>
    /// <returns>A selection that binds the named tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="toolNames" /> is empty or contains a null, empty, or whitespace name.
    /// </exception>
    public static AgentToolSelection FromNames(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        List<string> distinct = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string toolName in toolNames)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                throw new ArgumentException("Tool names must not be null, empty, or whitespace.", nameof(toolNames));
            }

            if (seen.Add(toolName))
            {
                distinct.Add(toolName);
            }
        }

        if (distinct.Count == 0)
        {
            throw new ArgumentException(
                $"A named tool selection requires at least one tool name. Use {nameof(AgentToolSelection)}.{nameof(None)} to bind no tools.",
                nameof(toolNames));
        }

        return new AgentToolSelection(AgentToolSelectionKind.Named, distinct, []);
    }





    /// <summary>
    ///     Creates a selection that binds every tool claimed by the supplied domains.
    /// </summary>
    /// <param name="domains">The domains whose tools should be bound. Duplicates are collapsed by name.</param>
    /// <returns>A selection that binds the union of the domain tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domains" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="domains" /> is empty.</exception>
    public static AgentToolSelection FromDomains(params AgentToolDomain[] domains)
    {
        return FromDomains((IEnumerable<AgentToolDomain>)domains);
    }





    /// <summary>
    ///     Creates a selection that binds every tool claimed by the supplied domains.
    /// </summary>
    /// <param name="domains">The domains whose tools should be bound. Duplicates are collapsed by name.</param>
    /// <returns>A selection that binds the union of the domain tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domains" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="domains" /> is empty.</exception>
    public static AgentToolSelection FromDomains(IEnumerable<AgentToolDomain> domains)
    {
        ArgumentNullException.ThrowIfNull(domains);

        List<AgentToolDomain> distinct = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (AgentToolDomain domain in domains)
        {
            ArgumentNullException.ThrowIfNull(domain);

            if (seen.Add(domain.Name))
            {
                distinct.Add(domain);
            }
        }

        if (distinct.Count == 0)
        {
            throw new ArgumentException(
                    $"A domain tool selection requires at least one domain. Use {nameof(AgentToolSelection)}.{nameof(None)} to bind no tools.",
                    nameof(domains));
        }

        return new AgentToolSelection(AgentToolSelectionKind.Domains, [], distinct);
    }





    /// <summary>
    ///     Creates a selection from the role toolset that belongs to the supplied preset.
    /// </summary>
    /// <param name="presetName">The preset whose role toolset should be bound.</param>
    /// <returns>
    ///     A domain selection for the role, or <see cref="None" /> when the role is defined as tool-free
    ///     (the Manager and the Classifier).
    /// </returns>
    public static AgentToolSelection FromRole(AgentPreset presetName)
    {
        IReadOnlyList<AgentToolDomain> domains = AgentToolRoleToolsets.For(presetName);

        return domains.Count == 0 ? None : FromDomains(domains);
    }





    /// <summary>
    ///     Returns a diagnostic representation of this selection.
    /// </summary>
    /// <returns>
    ///     The selection kind, plus the tool names when the selection is named or the domain names when the
    ///     selection is domain based.
    /// </returns>
    public override string ToString()
    {
        return Kind switch
        {
            AgentToolSelectionKind.Named => $"{Kind}({string.Join(", ", ToolNames)})",
            AgentToolSelectionKind.Domains => $"{Kind}({string.Join(", ", Domains.Select(domain => domain.Name))})",
            _ => Kind.ToString()
        };
    }
}





/// <summary>
///     The platform's tool-domain vocabulary.
/// </summary>
/// <remarks>
///     <para>
///         Every domain is expressed as <see cref="ToolMatchRule" /> instances over tool <em>names</em>, never as a
///         fixed list. Adding or renaming a tool therefore requires no catalogue edit: the tool is claimed by its
///         area prefix on the next resolution.
///     </para>
///     <para>
///         <see cref="Unclassified" /> is the safety net that makes tool loss structurally impossible. Any tool that
///         no domain claims is reported there and surfaced as a warning rather than being silently dropped.
///     </para>
///     <para>
///         Domains overlap on purpose. A registry ACL read belongs to both <see cref="Registry" /> and
///         <see cref="Permissions" />; the factory de-duplicates by tool name.
///     </para>
/// </remarks>
public static class AgentToolDomainCatalog
{
    /// <summary>Gets the domain for the command allow-list surface.</summary>
    public static AgentToolDomain AllowedCommands { get; } = new("AllowedCommands", "Inspects the configured command allow-list.", Area("AllowedCommands"));

    /// <summary>Gets the domain for audit-policy and effective-permission auditing.</summary>
    public static AgentToolDomain Auditing { get; } = new("Auditing", "Audit policy, effective permissions and access auditing.", Area("Auditing", ToolMatchRule.Token("AuditPol"), ToolMatchRule.Token("AccessChk")));

    /// <summary>Gets the domain for autorun and persistence enumeration.</summary>
    public static AgentToolDomain Autoruns { get; } = new("Autoruns", "Autorun locations and image-hijack persistence.", Area("Autoruns"));

    /// <summary>Gets the domain for BitLocker volume protection.</summary>
    public static AgentToolDomain Bitlocker { get; } = new("Bitlocker", "BitLocker status, protectors and recovery keys.", Area("Bitlocker"));

    /// <summary>Gets the domain for browser configuration and extensions.</summary>
    public static AgentToolDomain Browser { get; } = new("Browser", "Browser configuration, policies and installed extensions.", Area("Browser"));

    /// <summary>Gets the domain for the internal case-lifecycle tools.</summary>
    public static AgentToolDomain CaseLifecycle { get; } = new("CaseLifecycle", "Internal case creation and report persistence.", Area("local_case"));

    /// <summary>Gets the domain for certificate and signature inspection.</summary>
    public static AgentToolDomain Certificates { get; } = new("Certificates", "Code-signing certificates and signature verification.", Area("Certificate", ToolMatchRule.Token("Certificates"), ToolMatchRule.Token("SigCheck")));

    /// <summary>Gets the domain for COM registration inspection.</summary>
    public static AgentToolDomain Com { get; } = new("Com", "COM registration and component hijacking surfaces.", Area("COM"));

    /// <summary>Gets the domain for Defender antivirus state and history.</summary>
    public static AgentToolDomain Defender { get; } = new("Defender", "Defender status, scan history and threat history.", Area("Defender"));

    /// <summary>Gets the domain for plug-and-play device enumeration.</summary>
    public static AgentToolDomain Devices { get; } = new("Devices", "Plug-and-play devices and their state.", Area("Device", ToolMatchRule.Token("Devices"), ToolMatchRule.Token("PnP"), ToolMatchRule.Token("PnpListDevices")));

    /// <summary>Gets the domain for documentation and evidence-verification lookups.</summary>
    public static AgentToolDomain Documentation { get; } = new("Documentation", "Vendor documentation lookups used to verify evidence.", Area("local_docs", ToolMatchRule.Token("mslearn"), ToolMatchRule.Token("docs")));

    /// <summary>Gets the domain for kernel and file-system drivers.</summary>
    public static AgentToolDomain Driver { get; } = new("Driver", "Installed kernel and file-system drivers.", Area("Driver", ToolMatchRule.Token("Drivers")));

    /// <summary>Gets the domain for DNS resolution and cache inspection.</summary>
    public static AgentToolDomain Dns { get; } = new("Dns", "DNS resolution and resolver cache inspection.", Area("DNS"));

    /// <summary>Gets the domain for Windows event log access.</summary>
    public static AgentToolDomain EventLog { get; } = new("EventLog", "Event log channel enumeration and queries.", Area("Event_Log", ToolMatchRule.Token("EventLog")));

    /// <summary>Gets the domain for file-system evidence.</summary>
    public static AgentToolDomain FileSystem { get; } = new("FileSystem", "Files, directories, streams, hashes, ACLs and metadata.", Area("File_System", ToolMatchRule.Token("FileSystem")));

    /// <summary>Gets the domain for firewall configuration.</summary>
    public static AgentToolDomain Firewall { get; } = new("Firewall", "Firewall profiles and rule inspection.", Area("Firewall"));

    /// <summary>Gets the domain for Group Policy and resultant policy.</summary>
    public static AgentToolDomain GroupPolicy { get; } = new("GroupPolicy", "Group Policy objects and resultant set of policy.", Area("Group_Policy", ToolMatchRule.Token("GroupPolicy"), ToolMatchRule.Token("GPO")));

    /// <summary>Gets the domain for installed update inventory.</summary>
    public static AgentToolDomain Hotfix { get; } = new("Hotfix", "Installed hotfixes and update inventory.", Area("Hotfix", ToolMatchRule.Token("Hotfixes")));

    /// <summary>Gets the domain for logon activity.</summary>
    public static AgentToolDomain Logon { get; } = new("Logon", "Interactive and network logon activity.", Area("Logon"));

    /// <summary>Gets the domain for memory inspection.</summary>
    public static AgentToolDomain Memory { get; } = new("Memory", "Physical and virtual memory inspection.", Area("Memory", ToolMatchRule.Token("VMMap"), ToolMatchRule.Token("RAMMap")));

    /// <summary>Gets the domain for loaded module inspection.</summary>
    public static AgentToolDomain Modules { get; } = new("Modules", "Loaded modules and DLLs per process.", Area("Module", ToolMatchRule.Token("Modules"), ToolMatchRule.Token("ListDlls")));

    /// <summary>Gets the domain for network state.</summary>
    public static AgentToolDomain Network { get; } = new("Network", "Connections, ARP cache, routes and adapters.", Area("Network"));

    /// <summary>Gets the domain for permission and access-control inspection.</summary>
    public static AgentToolDomain Permissions { get; } = new("Permissions", "Access control lists and effective permissions.", ToolMatchRule.Token("Acl"), ToolMatchRule.Token("AccessChk"), ToolMatchRule.Token("Access"));

    /// <summary>Gets the domain for named-pipe inspection.</summary>
    public static AgentToolDomain Pipes { get; } = new("Pipes", "Named pipes and their instances.", Area("Pipe", ToolMatchRule.Token("Pipes"), ToolMatchRule.Token("PipeList")));

    /// <summary>Gets the domain for PowerShell execution surfaces.</summary>
    public static AgentToolDomain PowerShell { get; } = new("PowerShell", "PowerShell read-only execution and history.", Area("PowerShell"));

    /// <summary>Gets the domain for process inspection.</summary>
    public static AgentToolDomain Process { get; } = new("Process", "Process enumeration, handles and dumps.", Area("Process", ToolMatchRule.Token("PsList"), ToolMatchRule.Token("ProcDump"), ToolMatchRule.Token("Handle")));

    /// <summary>Gets the domain for registry evidence.</summary>
    public static AgentToolDomain Registry { get; } = new("Registry", "Registry keys, values and hives.", Area("Registry"));

    /// <summary>Gets the domain for scheduled task inspection.</summary>
    public static AgentToolDomain ScheduledTasks { get; } = new("ScheduledTasks", "Scheduled tasks and their triggers.", Area("ScheduledTasks", ToolMatchRule.Token("ScheduledTask")));

    /// <summary>Gets the domain for the Windows Search index.</summary>
    public static AgentToolDomain SearchIndex { get; } = new("SearchIndex", "Windows Search crawl scope and service settings.", Area("Search", ToolMatchRule.Token("SearchIndex")));

    /// <summary>Gets the domain for security configuration.</summary>
    public static AgentToolDomain Security { get; } = new("Security", "Security policy, zones and hardening state.", Area("Security"));

    /// <summary>Gets the domain for service inspection.</summary>
    public static AgentToolDomain Service { get; } = new("Service", "Installed services, their state and configuration.", Area("Service", ToolMatchRule.Token("Services")));

    /// <summary>Gets the domain for logon and terminal sessions.</summary>
    public static AgentToolDomain Sessions { get; } = new("Sessions", "Active logon and remote sessions.", Area("Sessions", ToolMatchRule.Token("LogonSessions")));

    /// <summary>Gets the domain for shell and taskbar configuration.</summary>
    public static AgentToolDomain ShellTaskbar { get; } = new("ShellTaskbar", "Shell and taskbar configuration surfaces.", Area("Shell_Taskbar", ToolMatchRule.Token("ShellTaskbar")));

    /// <summary>Gets the domain for installed software inventory.</summary>
    public static AgentToolDomain Software { get; } = new("Software", "Installed software inventory.", Area("Software"));

    /// <summary>Gets the domain for general system state.</summary>
    public static AgentToolDomain System { get; } = new("System", "System information and general machine state.", Area("System"));

    /// <summary>Gets the domain for access-token inspection.</summary>
    public static AgentToolDomain Tokens { get; } = new("Tokens", "Access token privileges and integrity level.", Area("Token", ToolMatchRule.Token("Tokens")));

    /// <summary>Gets the domain for User Account Control state.</summary>
    public static AgentToolDomain Uac { get; } = new("Uac", "User Account Control configuration and elevation state.", Area("UAC"));

    /// <summary>Gets the domain for volume and disk inspection.</summary>
    public static AgentToolDomain Volume { get; } = new("Volume", "Volumes, partitions and disk layout.", Area("Volume", ToolMatchRule.Token("Volumes")));

    /// <summary>Gets the domain for web research.</summary>
    public static AgentToolDomain WebResearch { get; } = new("WebResearch", "Web search and page retrieval for external context.", Area("Web", ToolMatchRule.Token("WebSearch"), ToolMatchRule.Token("WebResearch")));

    /// <summary>Gets the domain for WMI queries.</summary>
    public static AgentToolDomain Wmi { get; } = new("Wmi", "WMI namespaces, classes and queries.", Area("WMI"));





    /// <summary>
    ///     Gets every tool domain known to the platform.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> All { get; } =
    [
            AllowedCommands, Auditing, Autoruns, Bitlocker, Browser, CaseLifecycle, Certificates, Com, Defender,
            Devices, Documentation, Driver, Dns, EventLog, FileSystem, Firewall, GroupPolicy, Hotfix, Logon,
            Memory, Modules, Network, Permissions, Pipes, PowerShell, Process, Registry, ScheduledTasks,
            SearchIndex, Security, Service, Sessions, ShellTaskbar, Software, System, Tokens, Uac, Volume,
            WebResearch, Wmi
    ];





    /// <summary>
    ///     Gets the composite categories that group related domains for coarser assignment.
    /// </summary>
    /// <remarks>
    ///     A category is itself an <see cref="AgentToolDomain" /> whose rules are the union of its members, so it
    ///     flows through the same resolution path and needs no separate selection kind.
    /// </remarks>
    public static IReadOnlyList<AgentToolDomain> Categories { get; } =
    [
            Compose("Forensics", "Core forensic evidence surfaces.", FileSystem, Registry, EventLog, Process, Service, Driver, ScheduledTasks),
            Compose("Persistence", "Autostart and persistence mechanisms.", Autoruns, ScheduledTasks, Service, Driver, Com, ShellTaskbar),
            Compose("Identity", "Accounts, logons, sessions and tokens.", Security, Logon, Sessions, Tokens, Uac),
            Compose("NetworkAccess", "Network state and perimeter configuration.", Network, Firewall, Dns),
            Compose("Cryptography", "Certificates, signatures and volume encryption.", Certificates, Bitlocker),
            Compose("Configuration", "Policy, auditing and machine configuration.", GroupPolicy, Auditing, Wmi, System, AllowedCommands),
            Compose("Execution", "Execution surfaces and interpreters.", Process, PowerShell, Com, Wmi),
            Compose("MemoryInspection", "Memory, modules and inter-process channels.", Memory, Modules, Pipes),
            Compose("Hardware", "Devices, drivers, volumes and installed packages.", Devices, Driver, Volume, Hotfix, Software),
            Compose("Hardening", "Protective controls and their effectiveness.", Defender, Security, Auditing, Firewall, Permissions),
            Compose("CaseWork", "Internal case lifecycle and documentation lookups.", CaseLifecycle, Documentation),
            Compose("Utilities", "General machine state and diagnostics.", System, SearchIndex, Browser, Volume)
    ];





    /// <summary>
    ///     Returns the catalogue tools that no domain claims.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to test.</param>
    /// <returns>
    ///     The unclaimed tool names. A non-empty result is a warning, never an error: the tools are still
    ///     reachable through <see cref="AgentToolSelection.All" /> and by explicit name.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Unclassified(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        return [.. toolNames.Where(toolName => !All.Any(domain => domain.Matches(toolName)))];
    }





    /// <summary>
    ///     Builds the rule set for an area-owned tool-name stem.
    /// </summary>
    /// <param name="stem">The area stem, for example <c>Registry</c>, <c>Event_Log</c> or <c>local_case</c>.</param>
    /// <param name="extra">Additional rules for tools that deviate from the area naming convention.</param>
    /// <returns>
    ///     A prefix rule, plus a token rule when the stem is a single segment. The token rule keeps a
    ///     vendor-prefixed name such as <c>Sysinternals_PsList_List_Processes</c> claimable without naming
    ///     the vendor, and it means a domain cannot be emptied by a singular/plural change alone.
    /// </returns>
    private static ToolMatchRule[] Area(string stem, params ToolMatchRule[] extra)
    {
        List<ToolMatchRule> rules = [ToolMatchRule.Prefix(stem)];

        if (!stem.Contains('_'))
        {
            rules.Add(ToolMatchRule.Token(stem));
        }

        rules.AddRange(extra);

        return [.. rules];
    }





    /// <summary>
    ///     Builds a composite domain from the union of its member rules.
    /// </summary>
    /// <param name="name">The composite domain name.</param>
    /// <param name="description">A short human-readable description.</param>
    /// <param name="members">The domains whose rules are merged.</param>
    /// <returns>The composite domain.</returns>
    private static AgentToolDomain Compose(string name, string description, params AgentToolDomain[] members)
    {
        return new AgentToolDomain(name, description, [.. members.SelectMany(member => member.Rules)]);
    }
}





/// <summary>
///     Maps each agent role to the tool domains that role is allowed to use.
/// </summary>
/// <remarks>
///     Role toolsets are the clean way to build an agent: the call site names the role's job, not a list of tools.
///     A call site can still narrow or widen the toolset through <see cref="AgentToolSelection.FromNames" />.
/// </remarks>
public static class AgentToolRoleToolsets
{
    /// <summary>
    ///     Gets the Core reasoning toolset: case lifecycle, documentation lookup and web research only.
    ///     The Core is the human interface, so it deliberately holds no operating-system tools.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> CoreReasoning { get; } = [AgentToolDomainCatalog.CaseLifecycle, AgentToolDomainCatalog.Documentation, AgentToolDomainCatalog.WebResearch];

    /// <summary>
    ///     Gets the guardrail toolset used by the safety agent to check an action against the system's controls.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> Guardrails { get; } =
    [
            AgentToolDomainCatalog.Security, AgentToolDomainCatalog.Auditing, AgentToolDomainCatalog.Permissions,
            AgentToolDomainCatalog.Defender, AgentToolDomainCatalog.Firewall, AgentToolDomainCatalog.GroupPolicy,
            AgentToolDomainCatalog.Uac
    ];

    /// <summary>
    ///     Gets the investigative toolset used by worker agents.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> CaseWork { get; } =
    [
            AgentToolDomainCatalog.FileSystem, AgentToolDomainCatalog.Registry, AgentToolDomainCatalog.EventLog,
            AgentToolDomainCatalog.Process, AgentToolDomainCatalog.Service, AgentToolDomainCatalog.Driver,
            AgentToolDomainCatalog.ScheduledTasks
    ];

    /// <summary>
    ///     Gets the auxiliary toolset used by utility agents.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> Utilities { get; } =
    [
            AgentToolDomainCatalog.System, AgentToolDomainCatalog.SearchIndex, AgentToolDomainCatalog.Browser,
            AgentToolDomainCatalog.Volume
    ];

    /// <summary>
    ///     Gets the evidence-verification toolset used when a report is assembled.
    /// </summary>
    public static IReadOnlyList<AgentToolDomain> EvidenceVerification { get; } = [AgentToolDomainCatalog.Documentation];

    /// <summary>
    ///     Returns the toolset for the supplied preset.
    /// </summary>
    /// <param name="presetName">The preset whose role toolset should be returned.</param>
    /// <returns>
    ///     The role toolset, or an empty list when the role is tool-free. The Manager is tool-free by PL-3 and the
    ///     Classifier only routes input, so both return empty.
    /// </returns>
    public static IReadOnlyList<AgentToolDomain> For(AgentPreset presetName)
    {
        return presetName switch
        {
            AgentPreset.CoreAgent => CoreReasoning,
            AgentPreset.WorkerAgent => CaseWork,
            AgentPreset.UtilityAgent => Utilities,
            AgentPreset.SafetyAgent => Guardrails,
            AgentPreset.AggregatorAgent => EvidenceVerification,
            AgentPreset.ReportExtractorAgent => EvidenceVerification,
            AgentPreset.ManagerAgent => [],
            AgentPreset.ClassifierAgent => [],
            _ => []
        };
    }
}
