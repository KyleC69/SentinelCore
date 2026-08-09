// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ISafetyRule.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.SafetyEngine;





/// <summary>
///     A rule that evaluates a prompt for safety concerns.
///     Implementations are self-contained and stateless — they receive context and return a result.
/// </summary>
public interface ISafetyRule
{

    /// <summary>A description of what this rule checks.</summary>
    string Description { get; }

    /// <summary>The unique name of this rule.</summary>
    string Name { get; }








    /// <summary>
    ///     Evaluates the given context and returns a result indicating whether the prompt
    ///     should be allowed, warned, or blocked.
    /// </summary>
    /// <param name="context">The evaluation context containing the prompt messages.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="SafetyRuleResult" /> indicating the outcome.</returns>
    Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default);
}