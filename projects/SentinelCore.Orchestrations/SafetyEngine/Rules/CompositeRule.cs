// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CompositeRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A composite safety rule that evaluates multiple sub-rules and aggregates results.
///     This allows grouping related rules under a single named rule.
/// </summary>
public sealed class CompositeRule : ISafetyRule
{
    private readonly IReadOnlyList<ISafetyRule> _subRules;








    /// <summary>
    ///     Creates a new <see cref="CompositeRule" /> that evaluates all sub-rules.
    /// </summary>
    /// <param name="name">The unique name of this composite rule.</param>
    /// <param name="subRules">The sub-rules to evaluate.</param>
    /// <param name="description">A description of what this rule checks.</param>
    public CompositeRule(string name, IEnumerable<ISafetyRule> subRules, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(subRules);

        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? $"Composite rule evaluating {subRules.Count()} sub-rule(s).";
        _subRules = subRules.ToList().AsReadOnly();
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public async Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        List<SafetyRuleResult> results = new(_subRules.Count);

        foreach (ISafetyRule rule in _subRules)
        {
            SafetyRuleResult result = await rule.EvaluateAsync(context, cancellationToken);
            results.Add(result);

            // Short-circuit on first block
            if (result.Action == SafetyAction.Block)
            {
                return SafetyRuleResult.Block(Name, result.Severity, result.Reason);
            }
        }

        // If any sub-rule warned, propagate as a warning
        List<SafetyRuleResult> warnings = results.Where(r => r.Action == SafetyAction.Warn).ToList();
        if (warnings.Count > 0)
        {
            SafetySeverity highest = warnings.Max(r => r.Severity);
            return SafetyRuleResult.Warn(Name, highest, $"{warnings.Count} sub-rule(s) raised warnings.");
        }

        return SafetyRuleResult.Allow(Name);
    }








    /// <inheritdoc />
    public string Name { get; }
}