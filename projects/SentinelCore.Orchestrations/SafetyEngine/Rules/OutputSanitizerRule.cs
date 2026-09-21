// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         OutputSanitizerRule.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Text.RegularExpressions;




namespace SentinelCore.Orchestrations.SafetyEngine.Rules;





/// <summary>
///     A safety rule that sanitizes model output by removing noise patterns such as
///     thinking tags, markdown artifacts, and other model-generated artifacts that
///     should not appear in final responses.
/// </summary>
public sealed class OutputSanitizerRule : ISafetyRule
{
    private readonly IReadOnlyList<Regex> _customPatterns;
    private readonly bool _removeControlCharacters;
    private readonly bool _removeMarkdownArtifacts;
    private readonly bool _removeThinkingTags;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Initializes a new instance of the <see cref="OutputSanitizerRule" /> with default settings.
    /// </summary>
    public OutputSanitizerRule()
    {
        Name = "OutputSanitizer";
        Description = "Removes noise patterns from model output including thinking tags, markdown artifacts, and control characters.";
        _removeThinkingTags = true;
        _removeMarkdownArtifacts = true;
        _removeControlCharacters = true;
        _customPatterns = Array.Empty<Regex>();
        _severity = SafetySeverity.Low;
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="OutputSanitizerRule" /> with custom settings.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="removeThinkingTags">Whether to remove thinking tags (e.g., &lt;think&gt;, &lt;/think&gt;).</param>
    /// <param name="removeMarkdownArtifacts">Whether to remove incomplete markdown elements.</param>
    /// <param name="removeControlCharacters">Whether to remove control characters.</param>
    /// <param name="customPatterns">Additional regex patterns to remove from output.</param>
    /// <param name="severity">The severity when sanitization occurs. Default is <see cref="SafetySeverity.Low" />.</param>
    /// <param name="description">A description of what this rule checks.</param>
    public OutputSanitizerRule(string name, bool removeThinkingTags = true, bool removeMarkdownArtifacts = true, bool removeControlCharacters = true, IEnumerable<string>? customPatterns = null, SafetySeverity severity = SafetySeverity.Low, string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _removeThinkingTags = removeThinkingTags;
        _removeMarkdownArtifacts = removeMarkdownArtifacts;
        _removeControlCharacters = removeControlCharacters;
        _severity = severity;
        Description = description ?? "Removes noise patterns from model output.";

        if (customPatterns != null)
        {
            _customPatterns = customPatterns.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToList();
        }
        else
        {
            _customPatterns = Array.Empty<Regex>();
        }
    }









    public string Description { get; }









    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;
        bool wasModified = false;
        List<string> modifications = new();

        // Remove thinking tags like <think>, </think>, <thinking>, etc.
        if (_removeThinkingTags)
        {
            string before = text;
            text = RemoveThinkingTags(text);
            if (text != before)
            {
                wasModified = true;
                modifications.Add("Removed thinking tags");
            }
        }

        // Remove incomplete markdown artifacts
        if (_removeMarkdownArtifacts)
        {
            string before = text;
            text = RemoveMarkdownArtifacts(text);
            if (text != before)
            {
                wasModified = true;
                modifications.Add("Removed markdown artifacts");
            }
        }

        // Remove control characters
        if (_removeControlCharacters)
        {
            string before = text;
            text = RemoveControlCharacters(text);
            if (text != before)
            {
                wasModified = true;
                modifications.Add("Removed control characters");
            }
        }

        // Apply custom patterns
        foreach (Regex pattern in _customPatterns)
        {
            string before = text;
            text = pattern.Replace(text, string.Empty);
            if (text != before)
            {
                wasModified = true;
                modifications.Add($"Removed content matching pattern: {pattern}");
            }
        }

        if (wasModified)
        {
            string reason = modifications.Count > 0 ? $"Output sanitized: {string.Join(", ", modifications)}" : "Output contained noise patterns that were removed.";
            return Task.FromResult(SafetyRuleResult.Warn(Name, _severity, reason));
        }

        return Task.FromResult(SafetyRuleResult.Allow(Name, "No noise patterns detected in output."));
    }









    public string Name { get; }








    private static string RemoveControlCharacters(string text)
    {
        // Remove most control characters except for newline, tab, and carriage return
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty, RegexOptions.Compiled);
    }








    private static string RemoveMarkdownArtifacts(string text)
    {
        // Remove incomplete code blocks (starts with ``` but doesn't close)
        text = Regex.Replace(text, @"^```[a-z]*\s*$", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled);

        // Remove trailing incomplete lists
        text = Regex.Replace(text, @"^\s*[-*+]\s*$", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled);

        // Remove orphaned markdown emphasis markers
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)", string.Empty, RegexOptions.Compiled); // Single asterisks not part of **
        text = Regex.Replace(text, @"(?<!_)[_](?![_])", string.Empty, RegexOptions.Compiled); // Single underscores not part of __

        return text;
    }








    private static string RemoveThinkingTags(string text)
    {
        // Remove common thinking tag patterns
        text = Regex.Replace(text, @"<[/]?think(ing)?[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        text = Regex.Replace(text, @"<[/]?reflection[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        text = Regex.Replace(text, @"<[/]?analysis[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        text = Regex.Replace(text, @"\{\{.*?\}\}", string.Empty, RegexOptions.Compiled); // Handlebars-style templates
        return text;
    }
}
