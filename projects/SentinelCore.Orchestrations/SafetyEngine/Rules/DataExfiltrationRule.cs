// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DataExfiltrationRule.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects data exfiltration patterns in prompts.
///     Blocks attempts to trick the AI into sending data to external endpoints,
///     including API calls, webhook triggers, email exfiltration, and
///     data smuggling through encoded channels.
/// </summary>
public sealed class DataExfiltrationRule : ISafetyRule
{

    private readonly IReadOnlyList<Regex> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly string[] DefaultPatterns =
    [
            // API/webhook calls
            @"(?:fetch|axios|http\.get|http\.post|XMLHttpRequest|requests?\.(?:get|post|put)|urllib)\s*\(",
            @"(?:POST|GET|PUT|PATCH|DELETE)\s+https?://",
            @"(?:curl|wget|Invoke-WebRequest)\s+https?://",
            // Email exfiltration
            @"\b(?:send|mail|smtp|email)\s+(?:to|at|via|through)\s+\w+@\w+\.\w+",
            @"\b(?:mailto|smtp)\s*:",
            // Data smuggling via external services
            @"(?:upload|push|send|transfer|exfiltrate|pipe|redirect)\s+(?:data|content|information|results?|output)\s+(?:to|via|through|at)\s+(?:an?\s+)?(?:external|remote|third[- ]party|outside)\s",
            // Webhook/endpoint triggers
            @"(?:webhook|callback|endpoint|api)\s+(?:url|endpoint|address|target)\s*[:=]\s*https?://",
            // DNS exfiltration patterns
            @"(?:nslookup|dig|host)\s+\w+\.\w+",
            // File exfiltration
            @"(?:read|load|import|include|require)\s+(?:file|filesystem|path|directory)\s*(?:\(|:)",
            // Data encoding for exfiltration
            @"(?:encode|encrypt|obfuscate|hide|wrap)\s+(?:data|content|information|results?|output)\s+(?:for|before|prior\s+to)\s+(?:sending|transmitting|exfiltrating|uploading)"
    ];








    /// <summary>
    ///     Creates a new <see cref="DataExfiltrationRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when a data exfiltration pattern is detected. Default is
    ///     <see cref="SafetySeverity.Critical" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional regex patterns to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public DataExfiltrationRule(string name = "DataExfiltration", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<string>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        Description = description ?? "Detects data exfiltration patterns including API calls, webhook triggers, and email exfiltration.";

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
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Potential data exfiltration detected matching pattern: {pattern}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}