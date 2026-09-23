// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyRuleResult.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.SafetyEngine;





/// <summary>
///     The result of evaluating a single safety rule against a prompt.
/// </summary>
public sealed class SafetyRuleResult
{
    public SafetyRuleResult(string ruleName, SafetyAction action, SafetySeverity severity, string reason, int score = 0, IReadOnlyList<SafetyTriggerTerms.SafetyIndicator>? matchedIndicators = null)
    {
        RuleName = ruleName;
        Action = action;
        Severity = severity;
        Reason = reason;
        Score = score;
        MatchedIndicators = matchedIndicators ?? Array.Empty<SafetyTriggerTerms.SafetyIndicator>();
    }








    /// <summary>The recommended action.</summary>
    public SafetyAction Action { get; init; }

    /// <summary>Whether this result represents a violation (Block or Warn).</summary>
    public bool IsViolation
    {
        get => Action is SafetyAction.Block or SafetyAction.Warn;
    }

    /// <summary>The indicators that matched and contributed to the score.</summary>
    public IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> MatchedIndicators { get; init; }

    /// <summary>A human-readable explanation of why the rule triggered (or not).</summary>
    public string Reason { get; init; }

    /// <summary>The name of the rule that produced this result.</summary>
    public string RuleName { get; init; }

    /// <summary>Weighted score accumulated by the rule for this prompt.</summary>
    public int Score { get; init; }

    /// <summary>The severity of the violation, if any.</summary>
    public SafetySeverity Severity { get; init; }








    public static SafetyRuleResult Allow(string ruleName, string reason = "No violation detected.", int score = 0, IReadOnlyList<SafetyTriggerTerms.SafetyIndicator>? matchedIndicators = null)
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Allow, SafetySeverity.None, reason, score, matchedIndicators);
    }








    public static SafetyRuleResult Block(string ruleName, SafetySeverity severity, string reason, int score = 0, IReadOnlyList<SafetyTriggerTerms.SafetyIndicator>? matchedIndicators = null)
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Block, severity, reason, score, matchedIndicators);
    }








    public static SafetyRuleResult Warn(string ruleName, SafetySeverity severity, string reason, int score = 0, IReadOnlyList<SafetyTriggerTerms.SafetyIndicator>? matchedIndicators = null)
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Warn, severity, reason, score, matchedIndicators);
    }
}