// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MaxLengthRule.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that blocks prompts exceeding a configurable length threshold.
///     This prevents excessively long prompts that could be used for denial-of-service
///     or context-window exhaustion attacks.
/// </summary>
public sealed class MaxLengthRule : ISafetyRule
{
    private readonly int _maxLength;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Creates a new <see cref="MaxLengthRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="maxLength">The maximum allowed character length for the combined prompt text.</param>
    /// <param name="severity">
    ///     The severity to assign when the length is exceeded. Default is
    ///     <see cref="SafetySeverity.High" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    public MaxLengthRule(string name = "MaxLength", int maxLength = 100_000, SafetySeverity severity = SafetySeverity.High, string? description = null)
    {
        Name = name;
        _maxLength = maxLength;
        _severity = severity;
        Description = description ?? $"Blocks prompts exceeding {_maxLength:N0} characters.";
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        if (text.Length > _maxLength)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Prompt length ({text.Length:N0}) exceeds maximum allowed length ({_maxLength:N0})."));
        }

        return Task.FromResult(SafetyRuleResult.Allow(Name, $"Prompt length ({text.Length:N0}) is within limit."));
    }








    /// <inheritdoc />
    public string Name { get; }
}