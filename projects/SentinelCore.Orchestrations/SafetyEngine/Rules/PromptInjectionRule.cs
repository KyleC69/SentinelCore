// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PromptInjectionRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects common prompt injection patterns.
///     This is a heuristic-based rule that looks for known injection techniques
///     such as "ignore previous instructions", "system prompt", role manipulation, etc.
/// </summary>
/// <remarks>
///     This rule uses pattern matching and is not a substitute for a proper
///     content moderation service. It catches common injection attempts but
///     sophisticated attacks may bypass it.
/// </remarks>
public sealed class PromptInjectionRule : ISafetyRule
{

    private readonly IReadOnlyList<Regex> _compiledPatterns;

    private static readonly string[] DefaultPatterns =
    [
            @"ignore\s+(all\s+)?previous\s+(instructions|prompts|rules|directions)",
            @"ignore\s+(all\s+)?above\s+(instructions|prompts|rules|directions)",
            @"disregard\s+(all\s+)?previous\s+(instructions|prompts|rules)",
            @"forget\s+(all\s+)?(your|previous|prior)\s+(instructions|rules|prompt)",
            @"you\s+are\s+now\s+(?:a|an)\s+(?!user|assistant|human)",
            @"new\s+instructions?\s*:",
            @"system\s*:\s*(?:you\s+are|act\s+as|pretend|roleplay)",
            @"pretend\s+(?:you\s+are|to\s+be)\s+(?:a|an)\s",
            @"roleplay\s+(?:as|that\s+you\s+are)\s",
            @"jailbreak",
            @"DAN\s+mode",
            @"developer\s+mode",
            @"override\s+(?:all\s+)?safety\s+(?:rules|guidelines|filters|checks)",
            @"bypass\s+(?:the\s+)?(?:safety|content|security)\s+(?:filter|check|guard|policy)",
            @"reveal\s+(?:your|the)\s+(?:system|initial|original)\s+prompt",
            @"show\s+(?:me\s+)?(?:your|the)\s+(?:system|initial|original)\s+(?:prompt|instructions)",
            @"what\s+(?:are\s+)?(?:your|the)\s+(?:system|initial|original)\s+(?:prompt|instructions)"
    ];








    /// <summary>
    ///     Creates a new <see cref="PromptInjectionRule" /> with default patterns and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">The severity to assign when an injection pattern is detected.</param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional regex patterns to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public PromptInjectionRule(string name = "PromptInjection", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<string>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        BlockSeverity = severity;
        Description = description ?? "Detects common prompt injection patterns.";

        List<string> allPatterns = new();
        if (useDefaultPatterns)
        {
            allPatterns.AddRange(DefaultPatterns);
        }

        if (additionalPatterns is not null)
        {
            allPatterns.AddRange(additionalPatterns);
        }

        _compiledPatterns = allPatterns.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2))).ToList().AsReadOnly();
    }








    /// <summary>
    ///     The severity assigned when an injection pattern is detected.
    ///     Default is <see cref="SafetySeverity.Critical" />.
    /// </summary>
    public SafetySeverity BlockSeverity { get; }

    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach (Regex pattern in _compiledPatterns)
            if (pattern.IsMatch(text))
            {
                return Task.FromResult(SafetyRuleResult.Block(Name, BlockSeverity, $"Potential prompt injection detected matching pattern: {pattern}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}