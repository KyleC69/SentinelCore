// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentToolGroup.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Declares one tool group and the single name prefix that owns it.
/// </summary>
/// <remarks>
///     <para>
///         A group claims a tool purely by prefix. Every tool whose name starts with <see cref="Prefix" /> belongs
///         to the group; every other tool belongs to no group. There is exactly one prefix per group and exactly
///         one group per prefix, so the groups partition the prefixed name space and a tool can never be claimed
///         twice.
///     </para>
///     <para>
///         The prefix is derived from the group name and never declared independently: <see cref="Prefix" /> is
///         always <see cref="NamePrefixRoot" /> followed by the lower-cased <see cref="Name" /> followed by an
///         underscore. The naming scheme is the single source of truth for group ownership, which makes
///         duplicate-prefix and nested-prefix conflicts structurally impossible: two distinct group names can
///         never derive the same prefix, and because every prefix ends with its group name and an underscore, no
///         prefix can be a prefix of another.
///     </para>
///     <para>
///         Membership is evaluated against the live catalogue every time it is asked for, so a tool that is added
///         to or renamed on the MCP server is classified on the next resolution without any code change. A tool
///         that carries no registered prefix is reported as uncategorized when its name is foreign to the naming
///         convention, or as suspect when it starts with <see cref="NamePrefixRoot" /> but claims no group: it
///         looks like a Sentinel Core tool yet matches nothing, which is a naming defect or a malicious tool.
///     </para>
/// </remarks>
public sealed record AgentToolGroup
{
    /// <summary>
    ///     The mandatory root of every tool name the platform can classify.
    /// </summary>
    public const string NamePrefixRoot = "sc_";








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentToolGroup" /> class.
    /// </summary>
    /// <param name="name">The stable group name used by presets, call sites and diagnostics.</param>
    /// <param name="description">A short human-readable description of the group's purpose.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when any argument is null, empty, or whitespace, or when <paramref name="name" /> is not a single
    ///     name segment. The name is embedded between <see cref="NamePrefixRoot" /> and the trailing underscore to
    ///     form <see cref="Prefix" />, so it must not contain an underscore; otherwise the derived prefix would
    ///     declare a segment boundary that no longer matches the group name.
    /// </exception>
    public AgentToolGroup(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        string trimmed = name.Trim();

        if (trimmed.Contains('_'))
        {
            throw new ArgumentException($"The group name '{name}' must be a single name segment without '_'. The prefix is derived from the name as '{NamePrefixRoot}' + name + '_', so an underscore in the name would break the naming convention.", nameof(name));
        }

        Name = trimmed;
        Description = description;
        Prefix = $"{NamePrefixRoot}{trimmed.ToLowerInvariant()}_";
    }








    /// <summary>
    ///     Gets a short human-readable description of the group's purpose.
    /// </summary>
    public string Description { get; }



    /// <summary>
    ///     Gets the stable group name used by presets, call sites and diagnostics.
    /// </summary>
    public string Name { get; }



    /// <summary>
    ///     Gets the full name prefix owned by this group, including the <c>sc_</c> root and the trailing underscore.
    /// </summary>
    /// <remarks>
    ///     The prefix is never supplied by the caller. It is derived from <see cref="Name" /> as
    ///     <c>"sc_" + lower-cased name + "_"</c>, making the naming scheme the single source of truth for group
    ///     ownership. Comparison is case-insensitive, so casing in a tool name is never significant.
    /// </remarks>
    public string Prefix { get; }








    /// <summary>
    ///     Returns a diagnostic representation of this group.
    /// </summary>
    /// <returns>The group name followed by the owned prefix.</returns>
    public override string ToString()
    {
        return $"{Name}({Prefix})";
    }








    /// <summary>
    ///     Determines whether the supplied tool name belongs to this group.
    /// </summary>
    /// <param name="toolName">The tool name to test.</param>
    /// <returns>
    ///     <c>true</c> when the name starts with <see cref="Prefix" />; otherwise <c>false</c>. Comparison ignores
    ///     casing, so a server that emits <c>SC_FORENSICS_READ</c> is still claimed.
    /// </returns>
    public bool Claims(string? toolName)
    {
        return !string.IsNullOrWhiteSpace(toolName) && toolName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
    }
}