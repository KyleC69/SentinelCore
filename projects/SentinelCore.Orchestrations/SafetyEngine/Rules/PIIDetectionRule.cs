// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PIIDetectionRule.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects common Personally Identifiable Information (PII) patterns
///     in prompts, such as Social Security Numbers, credit card numbers, phone numbers,
///     and email addresses. Helps prevent accidental PII leakage to AI models.
/// </summary>
/// <remarks>
///     This rule uses regex-based heuristic detection and is not a substitute for a
///     dedicated PII detection service. It catches common formats but may produce
///     false positives or miss non-standard formats.
/// </remarks>
public sealed class PIIDetectionRule : ISafetyRule
{

    private readonly SafetyAction _action;

    private readonly IReadOnlyList<(string Label, Regex Regex)> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly (string Label, string Pattern)[] DefaultPatterns =
    [
            // US Social Security Number: XXX-XX-XXXX or XXX XX XXXX or XXXXXXXXX
            ("SSN", @"\b\d{3}[-\s]?\d{2}[-\s]?\d{4}\b"),
            // Credit card number: groups of 4 digits separated by spaces/dashes
            ("CreditCard", @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b"),
            // Email address
            ("Email", @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b"),
            // US Phone number: various formats
            ("Phone", @"\b(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b"),
            // IP Address (v4)
            ("IPv4", @"\b(?:\d{1,3}\.){3}\d{1,3}\b")
    ];








    /// <summary>
    ///     Creates a new <see cref="PIIDetectionRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">The severity to assign when PII is detected. Default is <see cref="SafetySeverity.High" />.</param>
    /// <param name="action">The action to take when PII is detected. Default is <see cref="SafetyAction.Warn" />.</param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional (Label, Pattern) tuples to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public PIIDetectionRule(string name = "PIIDetection", SafetySeverity severity = SafetySeverity.High, SafetyAction action = SafetyAction.Warn, string? description = null, IEnumerable<(string Label, string Pattern)>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        _action = action;
        Description = description ?? "Detects common PII patterns such as SSNs, credit card numbers, emails, and phone numbers.";

        List<(string Label, string Pattern)> allPatterns = new();
        if (useDefaultPatterns)
        {
            allPatterns.AddRange(DefaultPatterns);
        }

        if (additionalPatterns is not null)
        {
            allPatterns.AddRange(additionalPatterns);
        }

        _compiledPatterns = allPatterns.Select(p => (p.Label, new Regex(p.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2)))).ToList().AsReadOnly();
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;

        foreach ((string label, Regex regex) in _compiledPatterns)
            if (regex.IsMatch(text))
            {
                SafetyAction resultAction = _action;
                SafetyRuleResult result = resultAction switch
                {
                        SafetyAction.Block => SafetyRuleResult.Block(Name, _severity, $"Potential PII detected: {label}."),
                        SafetyAction.Warn => SafetyRuleResult.Warn(Name, _severity, $"Potential PII detected: {label}."),
                        _ => SafetyRuleResult.Allow(Name, $"PII pattern matched ({label}) but action is Allow.")
                };
                return Task.FromResult(result);
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name, "No PII patterns detected."));
    }








    /// <inheritdoc />
    public string Name { get; }
}