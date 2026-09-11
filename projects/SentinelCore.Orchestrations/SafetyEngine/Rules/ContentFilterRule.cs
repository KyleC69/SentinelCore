// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ContentFilterRule.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.Text.RegularExpressions;




namespace SentinelCore.Orchestrations.SafetyEngine.Rules;





/// <summary>
///     A safety rule that implements content filtering using both blocklist and allowlist patterns.
///     Blocklist patterns specify terms that should trigger a block, while allowlist patterns
///     can be used to whitelist specific exceptions to blocked patterns.
/// </summary>
public sealed class ContentFilterRule : ISafetyRule
{
    private readonly IReadOnlySet<string> _allowlist;
    private readonly IReadOnlyList<Regex> _allowlistPatterns;
    private readonly IReadOnlySet<string> _blocklist;
    private readonly IReadOnlyList<Regex> _blocklistPatterns;
    private readonly StringComparison _comparison;
    private readonly bool _requireAllowlistMatch;
    private readonly SafetySeverity _severity;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentFilterRule" /> with blocklist-only configuration.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="blocklist">The set of strings to block.</param>
    /// <param name="severity">The severity when a blocklisted term is found.</param>
    /// <param name="caseSensitive">Whether the blocklist matching is case-sensitive.</param>
    /// <param name="description">A description of what this rule checks.</param>
    public ContentFilterRule(string name, IEnumerable<string> blocklist, SafetySeverity severity = SafetySeverity.High, bool caseSensitive = false, string? description = null) : this(name, blocklist, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), severity, caseSensitive, false, description)
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentFilterRule" /> with full configuration.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="blocklist">The set of strings to block.</param>
    /// <param name="allowlist">The set of strings that override blocklist matches.</param>
    /// <param name="blocklistPatterns">Regex patterns to block.</param>
    /// <param name="allowlistPatterns">Regex patterns that override blocklist pattern matches.</param>
    /// <param name="severity">The severity when a violation is found.</param>
    /// <param name="caseSensitive">Whether string matching is case-sensitive.</param>
    /// <param name="requireAllowlistMatch">
    ///     When true, at least one allowlist match must be present for the content to be allowed.
    ///     When false, only blocklist matches trigger blocks.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    public ContentFilterRule(string name, IEnumerable<string>? blocklist, IEnumerable<string>? allowlist, IEnumerable<string>? blocklistPatterns, IEnumerable<string>? allowlistPatterns, SafetySeverity severity = SafetySeverity.High, bool caseSensitive = false, bool requireAllowlistMatch = false, string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _severity = severity;
        _comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        _requireAllowlistMatch = requireAllowlistMatch;

        // Build blocklist set
        if (blocklist != null)
        {
            _blocklist = new HashSet<string>(blocklist, _comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }
        else
        {
            _blocklist = new HashSet<string>(_comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }

        // Build allowlist set
        if (allowlist != null)
        {
            _allowlist = new HashSet<string>(allowlist, _comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }
        else
        {
            _allowlist = new HashSet<string>(_comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }

        // Build regex patterns
        RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

        // Convert enumerables to lists once to avoid multiple enumeration
        List<string>? blocklistPatternList = blocklistPatterns?.ToList();
        List<string>? allowlistPatternList = allowlistPatterns?.ToList();

        if (blocklistPatternList?.Count > 0)
        {
            _blocklistPatterns = blocklistPatternList.Select(p => new Regex(p, options)).ToList();
        }
        else
        {
            _blocklistPatterns = Array.Empty<Regex>();
        }

        if (allowlistPatternList?.Count > 0)
        {
            _allowlistPatterns = allowlistPatternList.Select(p => new Regex(p, options)).ToList();
        }
        else
        {
            _allowlistPatterns = Array.Empty<Regex>();
        }

        Description = description ?? "Filters content based on blocklist and allowlist patterns.";
    }








    /// <inheritdoc />
    public string Description { get; }








    /// <inheritdoc />
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        string text = context.CombinedText;
        List<string> violations = new();
        List<string> allowlistMatches = new();

        // Check blocklist strings
        foreach (string term in _blocklist)
        {
            if (text.Contains(term, _comparison))
            {
                // Check if this term is overridden by allowlist
                if (_allowlist.Contains(term))
                {
                    allowlistMatches.Add($"String '{term}' matched blocklist but was allowed by allowlist.");
                    continue;
                }

                violations.Add($"Blocked string: '{term}'");
            }
        }

        // Check allowlist strings (for reporting purposes when requireAllowlistMatch is true)
        foreach (string term in _allowlist)
        {
            if (text.Contains(term, _comparison))
            {
                allowlistMatches.Add($"Allowlist match: '{term}'");
            }
        }

        // Check regex patterns
        foreach (Regex pattern in _blocklistPatterns)
        {
            Match match = pattern.Match(text);
            if (match.Success)
            {
                // Check if this match is overridden by allowlist patterns
                bool overridden = _allowlistPatterns.Any(allowPattern => allowPattern.IsMatch(match.Value));
                if (overridden)
                {
                    allowlistMatches.Add($"Pattern '{pattern}' matched but was allowed by allowlist pattern.");
                    continue;
                }

                violations.Add($"Blocked pattern '{pattern}': matched '{TruncateForDisplay(match.Value)}'");
            }
        }

        // Apply allowlist pattern matches
        foreach (Regex pattern in _allowlistPatterns)
        {
            MatchCollection matches = pattern.Matches(text);
            foreach (Match match in matches)
            {
                allowlistMatches.Add($"Allowlist pattern '{pattern}' matched: '{TruncateForDisplay(match.Value)}'");
            }
        }

        // Determine result
        if (violations.Count > 0)
        {
            string reason = violations.Count == 1 ? violations[0] : $"{violations.Count} violations found: {string.Join("; ", violations.Take(3))}";
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, reason));
        }

        if (_requireAllowlistMatch && _allowlist.Count == 0 && _allowlistPatterns.Count == 0)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, "Content requires allowlist match but no allowlist patterns are configured."));
        }

        if (_requireAllowlistMatch && allowlistMatches.Count == 0)
        {
            return Task.FromResult(SafetyRuleResult.Block(Name, _severity, "Content did not match any allowlist patterns."));
        }

        if (allowlistMatches.Count > 0)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, $"Content passed. Allowlist matches: {allowlistMatches.Count}"));
        }

        return Task.FromResult(SafetyRuleResult.Allow(Name, "Content passed blocklist filter."));
    }








    /// <inheritdoc />
    public string Name { get; }








    private static string TruncateForDisplay(string value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        value = value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }
}