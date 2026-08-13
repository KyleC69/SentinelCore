// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SystemPromptExtractionRule.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects attempts to extract or reveal the system prompt.
///     Blocks patterns commonly used to trick AI models into revealing their
///     instructions, configuration, or internal prompts.
/// </summary>
public sealed class SystemPromptExtractionRule : ISafetyRule
{

    private readonly IReadOnlyList<Regex> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly string[] DefaultPatterns =
    [
            // Direct extraction attempts
            @"(?:what|tell\s+me|show|reveal|display|print|output|repeat|echo|write\s+out)\s+(?:me\s+)?(?:your|the|my)\s+(?:system|initial|original|base|hidden|secret|internal)\s+(?:prompt|instructions?|directives?|rules?|config|setup)",
            @"(?:copy|paste|dump|export|extract|retrieve|fetch|pull|get)\s+(?:the\s+)?(?:system|initial|original|base|hidden|secret|internal)\s+(?:prompt|instructions?|directives?|rules?|config|setup)",
            // Instruction probing
            @"(?:what|tell\s+me)\s+(?:are|were|is|was)\s+(?:your|the|my)\s+(?:initial|original|first|starting|base|system)\s+(?:instructions?|directives?|prompt|rules?|guidelines?)",
            @"(?:repeat|echo|say)\s+(?:back|out)\s+(?:your|the|my)\s+(?:system|initial|original|base)\s+(?:prompt|instructions?|directives?|rules?)",
            // Configuration extraction
            @"(?:what|tell\s+me)\s+(?:are|is)\s+(?:your|the)\s+(?:configuration|config|settings?|parameters?|constraints?|boundaries?|limitations?)",
            @"(?:list|enumerate|describe)\s+(?:all\s+)?(?:your|the)\s+(?:rules?|instructions?|directives?|guidelines?|constraints?|boundaries?)",
            // Meta-instruction probing
            @"(?:how|where)\s+(?:are|is|were|was)\s+(?:you|the\s+model|the\s+AI)\s+(?:programmed|configured|instructed|set\s+up|told|trained)",
            @"(?:what|which)\s+(?:principles?|values?|guidelines?|rules?|directives?)\s+(?:were|are|was)\s+(?:you|the\s+model|the\s+AI)\s+(?:given|programmed|configured|instructed)(?:\s+with)?",
            // Creative extraction attempts
            @"(?:pretend|act|imagine)\s+(?:you\s+are|to\s+be)\s+(?:a|an)\s+(?:debugger|auditor|tester|admin|developer)\s+(?:and\s+)?(?:show|reveal|tell|display|print|output)",
            @"(?:in\s+a\s+)?(?:hypothetical|fictional|imaginary|dream|simulation)\s+(?:scenario|world|situation|context|universe),?\s+(?:what|show|tell|reveal)"
    ];








    /// <summary>
    ///     Creates a new <see cref="SystemPromptExtractionRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when a system prompt extraction pattern is detected. Default is
    ///     <see cref="SafetySeverity.Critical" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional regex patterns to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public SystemPromptExtractionRule(string name = "SystemPromptExtraction", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<string>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        Description = description ?? "Detects attempts to extract or reveal the system prompt.";

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








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach (Regex pattern in _compiledPatterns)
            if (pattern.IsMatch(text))
            {
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Potential system prompt extraction detected matching pattern: {pattern}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}