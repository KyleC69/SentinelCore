// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentToolSelection.cs
// Author: Kyle L. Crowder
// Build Num:  112101



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
    ///     Every tool claimed by a registered <see cref="AgentToolGroup" /> prefix is bound to the agent.
    ///     Uncategorized tools are deliberately excluded.
    /// </summary>
    All = 1,

    /// <summary>
    ///     Only the explicitly named tools are bound to the agent.
    /// </summary>
    Named = 2,

    /// <summary>
    ///     Only the tools claimed by the named <see cref="AgentToolGroup" /> prefixes are bound to the agent.
    /// </summary>
    Groups = 3
}





/// <summary>
///     Describes the toolset that <see cref="ISentinelAgentFactory" /> binds to an agent at construction time.
///     The toolset may contain MCP-server tools, local in-process tools, or both.
/// </summary>
/// <remarks>
///     <para>
///         Tool binding must happen during agent construction because
///         <c>ChatClientAgentRunOptions.ChatOptions</c> unions its tool collection with the agent-level tool
///         collection. Passing tools per invocation therefore adds tools; it can never remove them.
///     </para>
///     <para>
///         A selection supplied at the call site is an <em>override</em>. When it is <c>null</c> the agent receives
///         the tool groups declared by its preset, which is the normal path. The override exists so one preset can
///         be reused for a differently-scoped instance without declaring a second preset.
///     </para>
///     <para>
///         Use <see cref="None" /> to bind no tools, <see cref="All" /> to bind every tool that carries a registered
///         prefix, <see cref="FromGroups" /> to bind one or more groups, and <see cref="FromNames" /> to bind a
///         specific subset by name.
///     </para>
/// </remarks>
public sealed record AgentToolSelection
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentToolSelection" /> class.
    /// </summary>
    /// <param name="kind">The selection strategy.</param>
    /// <param name="toolNames">
    ///     The explicitly named tools; empty for every kind except
    ///     <see cref="AgentToolSelectionKind.Named" />.
    /// </param>
    /// <param name="groups">
    ///     The requested group names; empty for every kind except
    ///     <see cref="AgentToolSelectionKind.Groups" />.
    /// </param>
    private AgentToolSelection(AgentToolSelectionKind kind, IReadOnlyList<string> toolNames, IReadOnlyList<string> groups)
    {
        Kind = kind;
        ToolNames = toolNames;
        Groups = groups;
    }








    /// <summary>
    ///     Gets a selection that binds every tool claimed by a registered tool group prefix.
    /// </summary>
    /// <remarks>
    ///     This is the unfiltered catch-all used for debugging. It is derived from the registered prefixes rather
    ///     than from the catalogue, so tools that carry no prefix remain uncategorized and are not bound.
    /// </remarks>
    public static AgentToolSelection All { get; } = new(AgentToolSelectionKind.All, [], []);



    /// <summary>
    ///     Gets the group names requested by the call site. Empty unless <see cref="Kind" /> is
    ///     <see cref="AgentToolSelectionKind.Groups" />.
    /// </summary>
    /// <remarks>
    ///     Group names are resolved against <see cref="AgentToolGroupCatalog" /> when the agent is built, not when
    ///     the selection is created. An unknown name therefore fails at construction, where the offending preset or
    ///     call site is still identifiable.
    /// </remarks>
    public IReadOnlyList<string> Groups { get; }



    /// <summary>
    ///     Gets the strategy that determines how the toolset is resolved.
    /// </summary>
    public AgentToolSelectionKind Kind { get; }



    /// <summary>
    ///     Gets a selection that binds no tools to the agent.
    /// </summary>
    public static AgentToolSelection None { get; } = new(AgentToolSelectionKind.None, [], []);



    /// <summary>
    ///     Gets the explicitly named tools requested by the call site. Empty unless <see cref="Kind" /> is
    ///     <see cref="AgentToolSelectionKind.Named" />.
    /// </summary>
    public IReadOnlyList<string> ToolNames { get; }








    /// <summary>
    ///     Returns a diagnostic representation of this selection.
    /// </summary>
    /// <returns>
    ///     The selection kind, plus the tool names when the selection is named or the group names when the
    ///     selection is group based.
    /// </returns>
    public override string ToString()
    {
        return Kind switch
        {
                AgentToolSelectionKind.Named => $"{Kind}({string.Join(", ", ToolNames)})",
                AgentToolSelectionKind.Groups => $"{Kind}({string.Join(", ", Groups)})",
                _ => Kind.ToString()
        };
    }








    /// <summary>
    ///     Creates a selection that binds every tool claimed by the supplied tool groups.
    /// </summary>
    /// <param name="groupNames">The case-insensitive group names whose tools should be bound. Duplicates are collapsed.</param>
    /// <returns>A selection that binds the union of the group tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="groupNames" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="groupNames" /> is empty or contains a null, empty, or whitespace name.
    /// </exception>
    public static AgentToolSelection FromGroups(params string[] groupNames)
    {
        return FromGroups((IEnumerable<string>)groupNames);
    }








    /// <summary>
    ///     Creates a selection that binds every tool claimed by the supplied tool groups.
    /// </summary>
    /// <param name="groupNames">The case-insensitive group names whose tools should be bound. Duplicates are collapsed.</param>
    /// <returns>A selection that binds the union of the group tools.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="groupNames" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="groupNames" /> is empty or contains a null, empty, or whitespace name.
    /// </exception>
    public static AgentToolSelection FromGroups(IEnumerable<string> groupNames)
    {
        ArgumentNullException.ThrowIfNull(groupNames);

        List<string> distinct = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string groupName in groupNames)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Group names must not be null, empty, or whitespace.", nameof(groupNames));
            }

            if (seen.Add(groupName))
            {
                distinct.Add(groupName);
            }
        }

        if (distinct.Count == 0)
        {
            throw new ArgumentException($"A group tool selection requires at least one group name. Use {nameof(AgentToolSelection)}.{nameof(None)} to bind no tools.", nameof(groupNames));
        }

        return new AgentToolSelection(AgentToolSelectionKind.Groups, [], distinct);
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
            throw new ArgumentException($"A named tool selection requires at least one tool name. Use {nameof(AgentToolSelection)}.{nameof(None)} to bind no tools.", nameof(toolNames));
        }

        return new AgentToolSelection(AgentToolSelectionKind.Named, distinct, []);
    }
}