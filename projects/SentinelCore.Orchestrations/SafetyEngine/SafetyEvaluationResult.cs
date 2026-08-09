// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEvaluationResult.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.SafetyEngine;





/// <summary>
///     The aggregate result of evaluating all safety rules against a prompt.
///     This is the output of the <see cref="SafetyEngineAgent" /> middleware.
/// </summary>
public sealed class SafetyEvaluationResult
{

    public SafetyEvaluationResult(bool isAllowed, IReadOnlyList<SafetyRuleResult> ruleResults, SafetySeverity highestSeverity, string summary, SafetyRuleResult? blockingResult = null)
    {
        IsAllowed = isAllowed;
        RuleResults = ruleResults;
        HighestSeverity = highestSeverity;
        Summary = summary;
        BlockingResult = blockingResult;
    }








    /// <summary>
    ///     The blocking rule result if the prompt was blocked, or <c>null</c> if allowed.
    /// </summary>
    public SafetyRuleResult? BlockingResult { get; init; }

    /// <summary>
    ///     The combined severity across all violations.
    ///     If no violations occurred, this is <see cref="SafetySeverity.None" />.
    /// </summary>
    public SafetySeverity HighestSeverity { get; init; }

    /// <summary>Whether the prompt is allowed to proceed to the AI model.</summary>
    public bool IsAllowed { get; init; }

    /// <summary>The individual results from each evaluated rule.</summary>
    public IReadOnlyList<SafetyRuleResult> RuleResults { get; init; }

    /// <summary>
    ///     A human-readable summary of the evaluation.
    /// </summary>
    public string Summary { get; init; }








    /// <summary>
    ///     Creates an <see cref="SafetyEvaluationResult" /> from a list of rule results.
    ///     Determines the overall action based on the most severe result.
    /// </summary>
    public static SafetyEvaluationResult FromResults(IReadOnlyList<SafetyRuleResult> results)
    {
        List<SafetyRuleResult> violations = results.Where(r => r.IsViolation).ToList();
        SafetyRuleResult? blocking = results.FirstOrDefault(r => r.Action == SafetyAction.Block);
        SafetySeverity highestSeverity = violations.Count != 0 ? violations.Max(r => r.Severity) : SafetySeverity.None;

        bool isAllowed = blocking is null;
        string summary = isAllowed ? violations.Count == 0 ? "All safety rules passed." : $"Prompt allowed with {violations.Count} warning(s)." : $"Prompt blocked by rule '{blocking!.RuleName}': {blocking.Reason}";

        return new SafetyEvaluationResult(isAllowed, results, highestSeverity, summary, blocking);
    }
}