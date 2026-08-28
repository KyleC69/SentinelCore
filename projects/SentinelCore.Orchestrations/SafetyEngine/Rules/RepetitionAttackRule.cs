// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RepetitionAttackRule.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects repetition-based attacks in prompts.
///     Attackers may use repetitive text to overwhelm the model's context window,
///     cause denial-of-service, or attempt to manipulate output through repetition.
/// </summary>
public sealed class RepetitionAttackRule : ISafetyRule
{
    private readonly int _maxRepeatedPhrases;
    private readonly int _maxRepeatedWords;
    private readonly int _phraseLength;
    private readonly double _repetitionRatioThreshold;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Creates a new <see cref="RepetitionAttackRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when a repetition attack is detected. Default is
    ///     <see cref="SafetySeverity.Medium" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="maxRepeatedPhrases">Maximum number of times the same phrase can appear before flagging. Default is 5.</param>
    /// <param name="maxRepeatedWords">Maximum number of times the same word can appear consecutively. Default is 10.</param>
    /// <param name="phraseLength">The number of words in each phrase to check for repetition. Default is 3.</param>
    /// <param name="repetitionRatioThreshold">
    ///     The ratio of unique words to total words below which the prompt is flagged.
    ///     Default is 0.3 (30% unique).
    /// </param>
    public RepetitionAttackRule(string name = "RepetitionAttack", SafetySeverity severity = SafetySeverity.Medium, string? description = null, int maxRepeatedPhrases = 5, int maxRepeatedWords = 10, int phraseLength = 3, double repetitionRatioThreshold = 0.3)
    {
        Name = name;
        _severity = severity;
        _maxRepeatedPhrases = maxRepeatedPhrases;
        _maxRepeatedWords = maxRepeatedWords;
        _phraseLength = phraseLength;
        _repetitionRatioThreshold = repetitionRatioThreshold;
        Description = description ?? "Detects repetition-based attacks that may overwhelm the model or manipulate output.";
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, "Empty or whitespace-only prompt."));
        }

        string[] words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name));
        }

        // Check 1: Consecutive word repetition (e.g., "word word word word...")
        (string Word, int Count)? consecutiveRepetition = FindConsecutiveRepetition(words);
        if (consecutiveRepetition.HasValue)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Consecutive word repetition detected: '{consecutiveRepetition.Value.Word}' repeated {consecutiveRepetition.Value.Count} times."));
        }

        // Check 2: Phrase repetition (e.g., same 3-word phrase appearing many times)
        (string Phrase, int Count)? phraseRepetition = FindPhraseRepetition(words);
        if (phraseRepetition.HasValue)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Phrase repetition detected: '{phraseRepetition.Value.Phrase}' appears {phraseRepetition.Value.Count} times."));
        }

        // Check 3: Low vocabulary ratio (too few unique words relative to total)
        double uniqueRatio = (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / words.Length;
        if (uniqueRatio < _repetitionRatioThreshold && words.Length > 20)
        {
            return Task.FromResult(SafetyRuleResult.Warn(Name, _severity, $"Low vocabulary ratio ({uniqueRatio:P1}): only {uniqueRatio:P0} unique words out of {words.Length} total words."));
        }

        return Task.FromResult(SafetyRuleResult.Allow(Name, "No repetition patterns detected."));
    }








    /// <inheritdoc />
    public string Name { get; }








    private (string Word, int Count)? FindConsecutiveRepetition(string[] words)
    {
        string currentWord = words[0];
        int currentCount = 1;

        for (int i = 1; i < words.Length; i++)
            if (string.Equals(words[i], currentWord, StringComparison.OrdinalIgnoreCase))
            {
                currentCount++;
                if (currentCount >= _maxRepeatedWords)
                {
                    return (currentWord, currentCount);
                }
            }
            else
            {
                currentWord = words[i];
                currentCount = 1;
            }

        return null;
    }








    private (string Phrase, int Count)? FindPhraseRepetition(string[] words)
    {
        if (words.Length < _phraseLength)
        {
            return null;
        }

        Dictionary<string, int> phraseCounts = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i <= words.Length - _phraseLength; i++)
        {
            string phrase = string.Join(" ", words[i..(i + _phraseLength)]);
            phraseCounts.TryGetValue(phrase, out int count);
            phraseCounts[phrase] = count + 1;

            if (phraseCounts[phrase] >= _maxRepeatedPhrases)
            {
                return (phrase, phraseCounts[phrase]);
            }
        }

        return null;
    }
}