// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BlocklistRule.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.SafetyEngine.Rules;





/// <summary>
///     A safety rule that evaluates weighted blocklist indicators in a prompt and returns
///     a cumulative score rather than a binary first-match result.
/// </summary>
public sealed class BlocklistRule : ISafetyRule
{

    private readonly IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> _indicators;
    private const int BlockThreshold = 15;
    private const int ReviewThreshold = 5;
    private const int WarningThreshold = 10;








    public BlocklistRule(string name, SafetySeverity severity = SafetySeverity.High, string? description = null) : this(name, severity, description, SafetyTriggerTerms.GetAllIndicators())
    {
    }








    public BlocklistRule(string name, IEnumerable<SafetyTriggerTerms.SafetyIndicator> indicators, SafetySeverity severity = SafetySeverity.High, string? description = null) : this(name, severity, description, indicators)
    {
    }








    public BlocklistRule(string name, IEnumerable<string> blocklist, SafetySeverity severity = SafetySeverity.High, string? description = null) : this(name, severity, description, NormalizeStringsToIndicators(blocklist))
    {
    }








    private BlocklistRule(string name, SafetySeverity severity, string? description, IEnumerable<SafetyTriggerTerms.SafetyIndicator>? indicators)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        Severity = severity;
        Description = description ?? "Blocks prompts containing blocklisted terms or phrases.";
        _indicators = NormalizeIndicators(indicators);
    }








    public SafetySeverity Severity { get; private set; }



    public string Description { get; }








    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string text = context.CombinedText;
        List<SafetyTriggerTerms.SafetyIndicator> matchedIndicators = _indicators.Where(indicator => text.Contains(indicator.Term, StringComparison.OrdinalIgnoreCase)).OrderByDescending(indicator => indicator.Weight).ToList();

        if (matchedIndicators.Count == 0)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name));
        }

        IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> uniqueIndicators = ReduceOverlappingIndicators(matchedIndicators);
        int score = uniqueIndicators.Sum(indicator => indicator.Weight);

        if (uniqueIndicators.Count > 1 && score < WarningThreshold)
        {
            score += uniqueIndicators.Count == 2 ? WarningThreshold - score : (uniqueIndicators.Count - 1) * 2;
        }

        bool requiresImmediateReview = uniqueIndicators.Any(indicator => indicator.RequiresImmediateReview);

        if (score >= BlockThreshold)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, SafetySeverity.Critical, $"Prompt exceeded the block threshold ({score}) with weighted indicators: {FormatIndicators(uniqueIndicators)}.", score, uniqueIndicators));
        }

        if (score >= WarningThreshold || (uniqueIndicators.Count >= 2 && score >= ReviewThreshold))
        {
            return Task.FromResult(SafetyRuleResult.Warn(Name, SafetySeverity.High, $"Prompt reached the warning threshold ({score}) with weighted indicators: {FormatIndicators(uniqueIndicators)}.", score, uniqueIndicators));
        }

        if (score >= ReviewThreshold || requiresImmediateReview || uniqueIndicators.Count > 0)
        {
            return Task.FromResult(SafetyRuleResult.Warn(Name, SafetySeverity.Medium, $"Prompt triggered weighted safety review ({score}) with indicators: {FormatIndicators(uniqueIndicators)}.", score, uniqueIndicators));
        }

        return Task.FromResult(SafetyRuleResult.Warn(Name, SafetySeverity.Medium, $"Prompt contains matched safety indicators ({score}) and should be reviewed.", score, uniqueIndicators));
    }








    public string Name { get; }








    private static string FormatIndicators(IEnumerable<SafetyTriggerTerms.SafetyIndicator> indicators)
    {
        return string.Join(", ", indicators.Select(indicator => $"{indicator.Term}({indicator.Weight})"));
    }








    private static IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> NormalizeIndicators(IEnumerable<SafetyTriggerTerms.SafetyIndicator>? indicators)
    {
        IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> fallback = SafetyTriggerTerms.GetAllIndicators().ToList();

        if (indicators is null)
        {
            return fallback;
        }

        List<SafetyTriggerTerms.SafetyIndicator> normalized = indicators.Where(indicator => !string.IsNullOrWhiteSpace(indicator.Term)).Select(indicator => indicator with { Term = indicator.Term.Trim() }).ToList();

        return normalized.Count > 0 ? normalized : fallback;
    }








    private static IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> NormalizeStringsToIndicators(IEnumerable<string>? blocklist)
    {
        if (blocklist is null)
        {
            return SafetyTriggerTerms.GetAllIndicators().ToList();
        }

        List<SafetyTriggerTerms.SafetyIndicator> indicators = blocklist.Where(term => !string.IsNullOrWhiteSpace(term)).Select(term => new SafetyTriggerTerms.SafetyIndicator(term.Trim(), "Blocklist", 2, false)).ToList();

        return indicators.Count > 0 ? indicators : SafetyTriggerTerms.GetAllIndicators().ToList();
    }








    private static IReadOnlyList<SafetyTriggerTerms.SafetyIndicator> ReduceOverlappingIndicators(IEnumerable<SafetyTriggerTerms.SafetyIndicator> indicators)
    {
        List<SafetyTriggerTerms.SafetyIndicator> retained = new();

        foreach (SafetyTriggerTerms.SafetyIndicator indicator in indicators.OrderByDescending(item => item.Weight))
        {
            SafetyTriggerTerms.SafetyIndicator? existing = retained.FirstOrDefault(item => TermsOverlap(item.Term, indicator.Term));
            if (existing is not null)
            {
                if (indicator.Weight > existing.Weight)
                {
                    int index = retained.IndexOf(existing);
                    retained[index] = indicator;
                }

                continue;
            }

            retained.Add(indicator);
        }

        return retained.OrderByDescending(indicator => indicator.Weight).ToList();
    }








    private static bool TermsOverlap(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        string normalizedLeft = left.Trim();
        string normalizedRight = right.Trim();

        return normalizedLeft.Contains(normalizedRight, StringComparison.OrdinalIgnoreCase) || normalizedRight.Contains(normalizedLeft, StringComparison.OrdinalIgnoreCase) || normalizedLeft.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Intersect(normalizedRight.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase).Any();
    }
}