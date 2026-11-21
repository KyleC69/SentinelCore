// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentToolGroupCatalog.cs
// Author: Kyle L. Crowder
// Build Num:  111504



namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     The platform's tool groups and the single name prefix each group owns.
/// </summary>
/// <remarks>
///     <para>
///         A group is defined by its prefix and nothing else. Any tool whose name starts with a registered prefix
///         belongs to that group automatically, so adding, removing or renaming a tool on the MCP server never
///         requires a catalogue edit and never silently drops a tool.
///     </para>
///     <para>
///         Because each prefix is derived from its group name and maps to exactly one group, the groups partition
///         the prefixed name space. A tool whose name does not start with the <c>sc_</c> root is not an error: it
///         is reported as uncategorized and stays reachable by explicit name. A tool that starts with the root but
///         that no group claims is suspect: it is reported as an error, is excluded from every grouping and from
///         resolution by name, and is therefore unreachable.
///     </para>
/// </remarks>
public static class AgentToolGroupCatalog
{
    /// <summary>Gets the group for case lifecycle and workflow surfaces owned by the Sentinel Core MCP server.</summary>
    public static AgentToolGroup CaseWork { get; } = new("CaseWork", "Case lifecycle, evidence intake and workflow surfaces owned by the Sentinel Core MCP server.");

    /// <summary>Gets the group for command and script execution surfaces.</summary>
    public static AgentToolGroup Execution { get; } = new("Execution", "Execution surfaces, interpreters and scripting hosts.");

    /// <summary>Gets the group for autostart and persistence mechanisms.</summary>
    public static AgentToolGroup Persistence { get; } = new("Persistence", "Autostart locations, scheduled tasks, services and other persistence mechanisms.");

    /// <summary>Gets the group for core forensic evidence surfaces.</summary>
    public static AgentToolGroup Forensics { get; } = new("Forensics", "Files, registry, event logs, processes and services that carry primary evidence.");

    /// <summary>Gets the group for memory, module and inter-process inspection.</summary>
    public static AgentToolGroup MemoryInspection { get; } = new("MemoryInspection", "Memory, loaded modules and inter-process channels.");

    /// <summary>Gets the group for accounts, logons, sessions and tokens.</summary>
    public static AgentToolGroup Identity { get; } = new("Identity", "Accounts, logons, sessions, access tokens and elevation state.");

    /// <summary>Gets the group for network state and perimeter configuration.</summary>
    public static AgentToolGroup NetworkAccess { get; } = new("NetworkAccess", "Connections, name resolution and perimeter configuration.");

    /// <summary>Gets the group for certificates, signatures and volume encryption.</summary>
    public static AgentToolGroup Cryptography { get; } = new("Cryptography", "Certificates, signature verification and volume encryption.");

    /// <summary>Gets the group for protective controls and their effectiveness.</summary>
    public static AgentToolGroup Hardening { get; } = new("Hardening", "Defensive controls, security policy and permission enforcement.");

    /// <summary>Gets the group for policy, auditing and machine configuration.</summary>
    public static AgentToolGroup Configuration { get; } = new("Configuration", "Group policy, auditing, instrumentation and machine configuration.");

    /// <summary>Gets the group for devices, drivers, volumes and installed packages.</summary>
    public static AgentToolGroup Hardware { get; } = new("Hardware", "Devices, drivers, volumes and installed packages.");

    /// <summary>Gets the group for general machine state and diagnostics.</summary>
    public static AgentToolGroup Utilities { get; } = new("Utilities", "General machine state, search index and installed software inventory.");

    /// <summary>Gets the group for external web research.</summary>
    public static AgentToolGroup WebResearch { get; } = new("WebResearch", "Web search and page retrieval used to obtain external context.");

    /// <summary>Gets the group for Microsoft Learn documentation search and retrieval.</summary>
    public static AgentToolGroup Knowledge { get; } = new("Knowledge", "Microsoft Learn documentation search and retrieval.");

    /// <summary>Gets the group for in-process Sentinel Core tools that are not hosted by an MCP server.</summary>
    public static AgentToolGroup Internal { get; } = new("Internal", "In-process Sentinel Core tools hosted by this application rather than an MCP server.");





    /// <summary>
    ///     Gets every tool group known to the platform, in declaration order.
    /// </summary>
    public static IReadOnlyList<AgentToolGroup> All { get; } =
    [
            CaseWork, Execution, Persistence, Forensics, MemoryInspection, Identity, NetworkAccess,
            Cryptography, Hardening, Configuration, Hardware, Utilities, WebResearch, Knowledge, Internal
    ];





    /// <summary>
    ///     Gets the names of every registered tool group.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } = [.. All.Select(group => group.Name)];





    /// <summary>
    ///     Initializes the catalog and rejects a group name that is registered twice.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The prefix is derived from the group name (<see cref="AgentToolGroup.NamePrefixRoot" /> followed by
    ///         the lower-cased name and an underscore), so two distinct names can never derive the same prefix and no
    ///         prefix can be a prefix of another. Duplicate-prefix and nested-prefix conflicts are therefore
    ///         structurally impossible and need no guard.
    ///     </para>
    ///     <para>
    ///         A duplicated name would make <see cref="Find" /> ambiguous and would derive a duplicated prefix, so
    ///         that is the only remaining defect. It fails at type initialization rather than producing a silently
    ///         wrong toolset at agent construction.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when a group name is registered twice.</exception>
    static AgentToolGroupCatalog()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (AgentToolGroup group in All)
        {
            if (!names.Add(group.Name))
            {
                throw new InvalidOperationException($"The tool group name '{group.Name}' is registered more than once.");
            }
        }
    }





    /// <summary>
    ///     Finds a tool group by name.
    /// </summary>
    /// <param name="name">The case-insensitive group name to find.</param>
    /// <returns>The matching group, or <c>null</c> when no group carries that name.</returns>
    public static AgentToolGroup? Find(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
                ? null
                : All.FirstOrDefault(group => string.Equals(group.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }





    /// <summary>
    ///     Finds a tool group by name and fails when it does not exist.
    /// </summary>
    /// <param name="name">The case-insensitive group name to find.</param>
    /// <returns>The matching group.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no group carries the supplied name. A preset or call site that names an unknown group is a
    ///     configuration defect and must be corrected, so this never degrades to a warning.
    /// </exception>
    public static AgentToolGroup FindRequired(string? name)
    {
        AgentToolGroup? group = Find(name);

        if (group is null)
        {
            throw new InvalidOperationException(
                    $"The tool group '{name}' is not registered. Valid group names are: {string.Join(", ", Names)}.");
        }

        return group;
    }





    /// <summary>
    ///     Selects the tools that some registered group claims.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to filter.</param>
    /// <returns>The claimed tool names in catalogue order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Claimed(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        return [.. toolNames.Where(toolName => All.Any(group => group.Claims(toolName)))];
    }





    /// <summary>
    ///     Selects the tools that no registered group claims and whose names do not carry the
    ///     <see cref="AgentToolGroup.NamePrefixRoot" /> root.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to test.</param>
    /// <returns>The uncategorized tool names in catalogue order.</returns>
    /// <remarks>
    ///     <para>
    ///         A non-empty result is a warning, never an error: these tools are foreign to the naming convention but
    ///         stay reachable by explicit name. They are deliberately excluded from
    ///         <see cref="AgentToolSelection.All" /> so that the catch-all selection stays a trustworthy debug view
    ///         of the conforming toolset.
    ///     </para>
    ///     <para>
    ///         Tools that carry the root but claim no group are not included here: they are suspect and are reported
    ///         by <see cref="Suspect" /> instead.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Uncategorized(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        return [.. toolNames.Where(toolName => !IsSuspect(toolName) && !All.Any(group => group.Claims(toolName)))];
    }




    /// <summary>
    ///     Selects the tools that carry the <see cref="AgentToolGroup.NamePrefixRoot" /> root but that no registered
    ///     group claims.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to test.</param>
    /// <returns>The suspect tool names in catalogue order.</returns>
    /// <remarks>
    ///     A suspect tool looks like a Sentinel Core tool but matches no group, which is either a naming defect or a
    ///     malicious tool added to the MCP server. Suspect tools are excluded from every grouping and from resolution
    ///     by name, so they are unreachable; reporting is an error, not a warning.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolNames" /> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Suspect(IEnumerable<string> toolNames)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        return [.. toolNames.Where(IsSuspect)];
    }




    /// <summary>
    ///     Determines whether the supplied tool name is suspect: it starts with
    ///     <see cref="AgentToolGroup.NamePrefixRoot" /> but no registered group claims it.
    /// </summary>
    /// <param name="toolName">The tool name to test.</param>
    /// <returns><c>true</c> when the name carries the root but matches no group; otherwise <c>false</c>.</returns>
    public static bool IsSuspect(string? toolName)
    {
        return !string.IsNullOrWhiteSpace(toolName)
                && toolName.StartsWith(AgentToolGroup.NamePrefixRoot, StringComparison.OrdinalIgnoreCase)
                && !All.Any(group => group.Claims(toolName));
    }
}
