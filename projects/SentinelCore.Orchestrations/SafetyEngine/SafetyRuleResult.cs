// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyRuleResult.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.SafetyEngine;





/// <summary>
///     The result of evaluating a single safety rule against a prompt.
/// </summary>
public sealed class SafetyRuleResult
{

    public SafetyRuleResult(string ruleName, SafetyAction action, SafetySeverity severity, string reason)
    {
        RuleName = ruleName;
        Action = action;
        Severity = severity;
        Reason = reason;
    }








    /// <summary>The recommended action.</summary>
    public SafetyAction Action { get; init; }

    /// <summary>Whether this result represents a violation (Block or Warn).</summary>
    public bool IsViolation
    {
        get => Action is SafetyAction.Block or SafetyAction.Warn;
    }

    /// <summary>A human-readable explanation of why the rule triggered (or not).</summary>
    public string Reason { get; init; }

    /// <summary>The name of the rule that produced this result.</summary>
    public string RuleName { get; init; }

    /// <summary>The severity of the violation, if any.</summary>
    public SafetySeverity Severity { get; init; }








    public static SafetyRuleResult Allow(string ruleName, string reason = "No violation detected.")
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Allow, SafetySeverity.None, reason);
    }








    public static SafetyRuleResult Block(string ruleName, SafetySeverity severity, string reason)
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Block, severity, reason);
    }








    public static SafetyRuleResult Warn(string ruleName, SafetySeverity severity, string reason)
    {
        return new SafetyRuleResult(ruleName, SafetyAction.Warn, severity, reason);
    }
}