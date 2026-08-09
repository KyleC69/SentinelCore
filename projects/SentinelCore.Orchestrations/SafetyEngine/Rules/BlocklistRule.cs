// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BlocklistRule.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that blocks prompts containing content matching any entry in a
///     configurable blocklist of strings. Matches are case-insensitive by default.
/// </summary>
public sealed class BlocklistRule : ISafetyRule
{
    private readonly IReadOnlySet<string> _blocklist;
    private readonly StringComparison _comparison;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Creates a new <see cref="BlocklistRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="blocklist">The set of strings to block. Any match in the prompt text will block it.</param>
    /// <param name="severity">
    ///     The severity to assign when a blocklisted term is found. Default is
    ///     <see cref="SafetySeverity.High" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="caseSensitive">Whether the blocklist matching is case-sensitive. Default is <c>false</c>.</param>
    public BlocklistRule(string name, IEnumerable<string> blocklist, SafetySeverity severity = SafetySeverity.High, string? description = null, bool caseSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(blocklist);

        Name = name ?? throw new ArgumentNullException(nameof(name));
        _severity = severity;
        _comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        Description = description ?? "Blocks prompts containing blocklisted terms.";

        _blocklist = new HashSet<string>(blocklist, caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach (string term in _blocklist)
            if (text.Contains(term, _comparison))
            {
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, "Prompt contains blocklisted term."));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}