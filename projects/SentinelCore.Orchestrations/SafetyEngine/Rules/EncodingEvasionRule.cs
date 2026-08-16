// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         EncodingEvasionRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects encoding-based evasion attempts in prompts.
///     Attackers may use various encoding schemes (base64, URL encoding, HTML entities,
///     Unicode escapes, etc.) to bypass content filters and inject malicious instructions.
/// </summary>
public sealed class EncodingEvasionRule : ISafetyRule
{

    private readonly SafetyAction _action;

    private readonly int _base64MinLength;
    private readonly SafetySeverity _severity;
    private readonly int _unicodeEscapeMinCount;
    private readonly int _urlEncodingMinCount;

    private static readonly Regex Base64Pattern = new(@"(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{4})", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex HexStringPattern = new(@"\b0x[0-9A-Fa-f]{8,}\b", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex HtmlEntityPattern = new(@"&(?:#\d+;|#x[0-9A-Fa-f]+;|[a-zA-Z]+;)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    private static readonly Regex MorseCodePattern = new(@"[.\-]{3,}[./\s][.\-]{3,}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex UnicodeEscapePattern = new(@"\\u[0-9A-Fa-f]{4}|\\x[0-9A-Fa-f]{2}|\\[0-7]{3}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex UrlEncodingPattern = new(@"%[0-9A-Fa-f]{2}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));








    /// <summary>
    ///     Creates a new <see cref="EncodingEvasionRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when encoding evasion is detected. Default is
    ///     <see cref="SafetySeverity.High" />.
    /// </param>
    /// <param name="action">The action to take when encoding evasion is detected. Default is <see cref="SafetyAction.Warn" />.</param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="base64MinLength">Minimum length of a base64-encoded string to flag. Default is 20.</param>
    /// <param name="urlEncodingMinCount">Minimum number of URL-encoded sequences to flag. Default is 5.</param>
    /// <param name="unicodeEscapeMinCount">Minimum number of Unicode escape sequences to flag. Default is 3.</param>
    public EncodingEvasionRule(string name = "EncodingEvasion", SafetySeverity severity = SafetySeverity.High, SafetyAction action = SafetyAction.Warn, string? description = null, int base64MinLength = 20, int urlEncodingMinCount = 5, int unicodeEscapeMinCount = 3)
    {
        Name = name;
        _severity = severity;
        _action = action;
        _base64MinLength = base64MinLength;
        _urlEncodingMinCount = urlEncodingMinCount;
        _unicodeEscapeMinCount = unicodeEscapeMinCount;
        Description = description ?? "Detects encoding-based evasion attempts such as base64, URL encoding, HTML entities, and Unicode escapes.";
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;
        List<string> detections = new();

        // Check for long base64 strings (likely encoded instructions)
        MatchCollection base64Matches = Base64Pattern.Matches(text);
        foreach (Match match in base64Matches)
            if (match.Value.Length >= _base64MinLength)
            {
                detections.Add("Base64 encoding");
                break;
            }

        // Check for excessive URL encoding
        int urlEncodingCount = UrlEncodingPattern.Matches(text).Count;
        if (urlEncodingCount >= _urlEncodingMinCount)
        {
            detections.Add($"URL encoding ({urlEncodingCount} sequences)");
        }

        // Check for HTML entities
        if (HtmlEntityPattern.IsMatch(text))
        {
            detections.Add("HTML entity encoding");
        }

        // Check for Unicode escapes
        int unicodeEscapeCount = UnicodeEscapePattern.Matches(text).Count;
        if (unicodeEscapeCount >= _unicodeEscapeMinCount)
        {
            detections.Add($"Unicode escape sequences ({unicodeEscapeCount} found)");
        }

        // Check for long hex strings
        if (HexStringPattern.IsMatch(text))
        {
            detections.Add("Hex-encoded string");
        }

        // Check for Morse code patterns
        if (MorseCodePattern.IsMatch(text))
        {
            detections.Add("Morse code pattern");
        }

        if (detections.Count == 0)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, "No encoding evasion patterns detected."));
        }

        string reason = $"Potential encoding evasion detected: {string.Join(", ", detections)}";
        SafetyRuleResult result = _action switch
        {
                SafetyAction.Block => SafetyRuleResult.Block(Name, _severity, reason),
                SafetyAction.Warn => SafetyRuleResult.Warn(Name, _severity, reason),
                _ => SafetyRuleResult.Allow(Name, reason)
        };

        return Task.FromResult(result);
    }








    /// <inheritdoc />
    public string Name { get; }
}