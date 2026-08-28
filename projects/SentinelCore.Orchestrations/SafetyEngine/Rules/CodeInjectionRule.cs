// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CodeInjectionRule.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects code injection patterns in prompts.
///     Blocks attempts to inject executable code (SQL, shell commands, script tags,
///     and other code execution patterns) that could be harmful if processed by
///     downstream systems.
/// </summary>
public sealed class CodeInjectionRule : ISafetyRule
{

    private readonly IReadOnlyList<Regex> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly string[] DefaultPatterns =
    [
            // SQL injection patterns
            @";\s*(?:DROP|DELETE|TRUNCATE|ALTER|CREATE|INSERT|UPDATE|EXEC)\s",
            @"'\s*(?:OR|AND)\s+['\d]",
            @"UNION\s+(?:ALL\s+)?SELECT",
            @"--\s*$", // SQL comment
            // Script/HTML injection
            @"<script[^>]*>",
            @"javascript\s*:",
            @"on(?:error|load|click|mouseover|focus)\s*=",
            // Shell command injection
            @"\b(?:rm\s+-rf|del\s+/[sqa]|format\s+[a-z]:)",
            @"\|\s*(?:bash|sh|cmd|powershell|python|perl|ruby)\b",
            @"\b(?:curl|wget)\s+https?://",
            // Code execution patterns
            @"\beval\s*\(",
            @"\bexec(?:ute)?\s*\(",
            @"\bsystem\s*\(",
            @"\bpassthru\s*\(",
            @"\bshell_exec\s*\("
    ];








    /// <summary>
    ///     Creates a new <see cref="CodeInjectionRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when a code injection pattern is detected. Default is
    ///     <see cref="SafetySeverity.Critical" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional regex patterns to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public CodeInjectionRule(string name = "CodeInjection", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<string>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        Description = description ?? "Detects code injection patterns including SQL injection, script injection, and shell command injection.";

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
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Potential code injection detected matching pattern: {pattern}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}