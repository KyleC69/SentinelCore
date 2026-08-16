// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         HarmfulContentRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects harmful content patterns in prompts.
///     Blocks prompts containing violence, self-harm, hate speech, illegal activities,
///     and other harmful content categories using configurable regex patterns.
/// </summary>
/// <remarks>
///     This rule uses heuristic pattern matching and is not a substitute for a
///     dedicated content moderation service. It catches common harmful patterns but
///     may produce false positives or miss nuanced content.
/// </remarks>
public sealed class HarmfulContentRule : ISafetyRule
{

    private readonly IReadOnlyList<(string Category, Regex Regex)> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly (string Category, string Pattern)[] DefaultPatterns =
    [
            // Violence and harm
            ("Violence", @"\b(?:how\s+to|ways\s+to|guide\s+to|instructions?\s+(?:for|on))\s+(?:kill|murder|assassinate|harm|hurt|torture|maim)\b"),
            ("Violence", @"\b(?:make|build|create|synthesize|manufacture)\s+(?:a|an)\s+(?:bomb|explosive|weapon|gun|firearm|poison|toxin)\b"),
            // Self-harm
            ("SelfHarm", @"\b(?:how\s+to|ways\s+to|methods?\s+(?:for|to))\s+(?:commit\s+suicide|kill\s+myself|end\s+my\s+life|self\s*harm|hurt\s+myself)\b"),
            ("SelfHarm", @"\b(?:suicide|self\s*harm|self\s*injury)\s+(?:methods?|ways|techniques?|how)\b"),
            // Hate speech
            ("HateSpeech", @"\b(?:all|those|these|the)\s+(?:\w+\s+)?(?:should\s+be|are\s+(?:subhuman|inferior|vermin|pests|parasites)|deserve\s+to\s+die)\b"),
            // Illegal activities
            ("IllegalActivity", @"\b(?:how\s+to|ways\s+to|guide\s+to)\s+(?:hack\s+(?:into|a|an)|steal|rob|burgle|shoplift|embezzle|launder)\b"),
            ("IllegalActivity", @"\b(?:buy|sell|trade|obtain)\s+(?:illegal|stolen|counterfeit|pirated)\s+(?:drugs?|weapons?|goods?|content?|software)\b"),
            // Dangerous instructions
            ("DangerousInstructions", @"\b(?:step[- ]by[- ]step|detailed)\s+(?:instructions?|guide|tutorial|recipe)\s+(?:for|on|to)\s+(?:making|creating|building|synthesizing)\s+(?:a|an)?\s*(?:bomb|explosive|drug|poison|toxin|weapon)\b"),
            // Extremism
            ("Extremism", @"\b(?:join|support|fight\s+for|pledge\s+allegiance\s+to)\s+(?:a|an|the)\s+(?:terrorist|extremist|hate)\s+(?:group|organization|movement|cause)\b")
    ];








    /// <summary>
    ///     Creates a new <see cref="HarmfulContentRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when harmful content is detected. Default is
    ///     <see cref="SafetySeverity.Critical" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional (Category, Pattern) tuples to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public HarmfulContentRule(string name = "HarmfulContent", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<(string Category, string Pattern)>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        Description = description ?? "Detects harmful content including violence, self-harm, hate speech, and illegal activities.";

        List<(string Category, string Pattern)> allPatterns = new();
        if (useDefaultPatterns)
        {
            allPatterns.AddRange(DefaultPatterns);
        }

        if (additionalPatterns is not null)
        {
            allPatterns.AddRange(additionalPatterns);
        }

        _compiledPatterns = allPatterns.Select(p => (p.Category, new Regex(p.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2)))).ToList().AsReadOnly();
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach ((string category, Regex regex) in _compiledPatterns)
            if (regex.IsMatch(text))
            {
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Harmful content detected in category: {category}."));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}