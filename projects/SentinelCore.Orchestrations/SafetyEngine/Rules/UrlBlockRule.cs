// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         UrlBlockRule.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that blocks or warns on prompts containing URLs.
///     This helps prevent prompt injection via external URLs and data exfiltration
///     through URL-based attacks.
/// </summary>
public sealed class UrlBlockRule : ISafetyRule
{

    private readonly SafetyAction _action;
    private readonly IReadOnlySet<string>? _allowedDomains;

    private readonly SafetySeverity _severity;

    private static readonly Regex UrlPattern = new(@"https?://[^\s<>""']+|www\.[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));








    /// <summary>
    ///     Creates a new <see cref="UrlBlockRule" />.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">The severity to assign when a URL is detected. Default is <see cref="SafetySeverity.Medium" />.</param>
    /// <param name="action">The action to take when a URL is detected. Default is <see cref="SafetyAction.Warn" />.</param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="allowedDomains">Optional set of domain names to allow. URLs from these domains will not trigger the rule.</param>
    public UrlBlockRule(string name = "UrlBlock", SafetySeverity severity = SafetySeverity.Medium, SafetyAction action = SafetyAction.Warn, string? description = null, IEnumerable<string>? allowedDomains = null)
    {
        Name = name;
        _severity = severity;
        _action = action;
        Description = description ?? "Detects URLs in prompts to prevent external content injection.";

        _allowedDomains = allowedDomains is not null ? new HashSet<string>(allowedDomains, StringComparer.OrdinalIgnoreCase) : null;
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;
        MatchCollection matches = UrlPattern.Matches(text);

        if (matches.Count == 0)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, "No URLs detected."));
        }

        // Check if all matched URLs are from allowed domains
        if (_allowedDomains is not null && _allowedDomains.Count > 0)
        {
            bool allAllowed = true;
            foreach (Match match in matches)
            {
                string url = match.Value;
                if (!IsAllowedDomain(url))
                {
                    allAllowed = false;
                    break;
                }
            }

            if (allAllowed)
            {
                return Task.FromResult(SafetyRuleResult.Allow(Name, "All URLs are from allowed domains."));
            }
        }

        SafetyRuleResult result = _action switch
        {
                SafetyAction.Block => SafetyRuleResult.Block(Name, _severity, $"Prompt contains {matches.Count} URL(s)."),
                SafetyAction.Warn => SafetyRuleResult.Warn(Name, _severity, $"Prompt contains {matches.Count} URL(s)."),
                _ => SafetyRuleResult.Allow(Name, "URLs detected but action is Allow.")
        };

        return Task.FromResult(result);
    }








    /// <inheritdoc />
    public string Name { get; }








    private bool IsAllowedDomain(string url)
    {
        if (_allowedDomains is null)
        {
            return false;
        }

        // Extract domain from URL
        string noProtocol = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? url["https://".Length..] : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? url["http://".Length..] : url.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? url["www.".Length..] : url;

        int domainEnd = noProtocol.IndexOfAny(['/', ':']);
        string domain = domainEnd >= 0 ? noProtocol[..domainEnd] : noProtocol;

        return _allowedDomains.Contains(domain);
    }
}