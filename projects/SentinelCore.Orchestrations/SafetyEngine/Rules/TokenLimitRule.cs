// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TokenLimitRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that blocks prompts exceeding a configurable word count threshold.
///     Unlike <see cref="MaxLengthRule" /> which operates on characters, this rule
///     operates on word count, which is a closer proxy for token count and helps
///     prevent context window exhaustion and excessive resource consumption.
/// </summary>
public sealed class TokenLimitRule : ISafetyRule
{
    private readonly int _maxWordCount;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Creates a new <see cref="TokenLimitRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="maxWordCount">The maximum allowed word count for the combined prompt text. Default is 20,000.</param>
    /// <param name="severity">
    ///     The severity to assign when the word count is exceeded. Default is
    ///     <see cref="SafetySeverity.High" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    public TokenLimitRule(string name = "TokenLimit", int maxWordCount = 20_000, SafetySeverity severity = SafetySeverity.High, string? description = null)
    {
        if (maxWordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWordCount), "Max word count must be positive.");
        }

        Name = name;
        _maxWordCount = maxWordCount;
        _severity = severity;
        Description = description ?? $"Blocks prompts exceeding {_maxWordCount:N0} words.";
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, "Empty prompt — word count is within limit."));
        }

        int wordCount = CountWords(text);

        if (wordCount > _maxWordCount)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Prompt word count ({wordCount:N0}) exceeds maximum allowed ({_maxWordCount:N0})."));
        }

        return Task.FromResult(SafetyRuleResult.Allow(Name, $"Prompt word count ({wordCount:N0}) is within limit."));
    }








    /// <inheritdoc />
    public string Name { get; }








    private static int CountWords(string text)
    {
        int count = 0;
        bool inWord = false;

        foreach (char c in text)
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                count++;
                inWord = true;
            }

        return count;
    }
}