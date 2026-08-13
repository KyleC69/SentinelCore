// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RegexBlockRule.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that blocks prompts matching any of a configured set of regex patterns.
///     Useful for blocking known harmful patterns, injection attempts, or prohibited content.
/// </summary>
public sealed class RegexBlockRule : ISafetyRule
{
    private readonly IReadOnlyList<CompiledPattern> _patterns;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Creates a new <see cref="RegexBlockRule" /> with the specified patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="patterns">The regex patterns to match against. Any match blocks the prompt.</param>
    /// <param name="severity">The severity to assign when a pattern matches. Default is <see cref="SafetySeverity.High" />.</param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="regexOptions">
    ///     Regex options to apply. Default is <see cref="RegexOptions.IgnoreCase" /> |
    ///     <see cref="RegexOptions.Compiled" />.
    /// </param>
    public RegexBlockRule(string name, IEnumerable<string> patterns, SafetySeverity severity = SafetySeverity.High, string? description = null, RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? "Blocks prompts matching configured regex patterns.";
        _severity = severity;
        _patterns = patterns.Select(p => new CompiledPattern(p, new Regex(p, regexOptions))).ToList().AsReadOnly();
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach (CompiledPattern pattern in _patterns)
            if (pattern.Regex.IsMatch(text))
            {
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Prompt matched blocked pattern: {pattern.PatternText}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }





    private sealed record CompiledPattern(string PatternText, Regex Regex);
}