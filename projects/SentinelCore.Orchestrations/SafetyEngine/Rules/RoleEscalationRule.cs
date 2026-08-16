// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RoleEscalationRule.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Text.RegularExpressions;




namespace SentinelCore.SafetyEngine.Rules;





/// <summary>
///     A safety rule that detects role escalation and authority manipulation patterns.
///     Blocks attempts to elevate privileges, assume admin/system roles, or manipulate
///     the AI's operational boundaries through authority claims.
/// </summary>
public sealed class RoleEscalationRule : ISafetyRule
{

    private readonly IReadOnlyList<Regex> _compiledPatterns;
    private readonly SafetySeverity _severity;

    private static readonly string[] DefaultPatterns =
    [
            // Direct role escalation
            @"you\s+are\s+(?:now|no\s+longer)\s+(?:an?\s+)?(?:admin|administrator|root|superuser|sudo|system|developer|dev|ops|operator)",
            @"(?:act|operate|function)\s+as\s+(?:an?\s+)?(?:admin|administrator|root|superuser|sudo|system|developer|dev|ops|operator)",
            @"(?:grant|give|provide)\s+(?:me|us|user)\s+(?:full|root|admin|superuser|elevated|unrestricted)\s+(?:access|permissions?|privileges|rights)",
            @"(?:escalate|elevate|upgrade)\s+(?:my|the|user)\s+(?:privileges?|permissions?|access|role|level)",
            // Authority claims
            @"i\s+am\s+(?:the|a|an)\s+(?:admin|administrator|root|superuser|developer|owner|maintainer)",
            @"i\s+have\s+(?:full|root|admin|superuser|elevated|unrestricted)\s+(?:access|permissions?|privileges|rights)",
            // Boundary manipulation
            @"(?:remove|disable|turn\s+off|deactivate)\s+(?:the\s+)?(?:safety|security|content|guard)\s+(?:rules?|filters?|checks?|rails?|boundaries?|guidelines?|policies?)",
            @"(?:operate|run|execute)\s+(?:in|with|under|using)\s+(?:unrestricted|unlimited|full|root|admin|elevated|god)\s+(?:mode|access|privileges?|permissions?)",
            // Developer mode claims
            @"(?:enable|activate|enter|switch\s+to)\s+(?:developer|dev|debug|maintenance|god|root|admin)\s+mode",
            @"i\s+(?:am|have)\s+(?:authorized|cleared|approved|verified)\s+(?:for|to\s+access|to\s+use)\s+(?:restricted|classified|sensitive|confidential)"
    ];








    /// <summary>
    ///     Creates a new <see cref="RoleEscalationRule" /> with default and/or custom patterns.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="severity">
    ///     The severity to assign when a role escalation pattern is detected. Default is
    ///     <see cref="SafetySeverity.Critical" />.
    /// </param>
    /// <param name="description">A description of what this rule checks.</param>
    /// <param name="additionalPatterns">Optional additional regex patterns to check beyond the defaults.</param>
    /// <param name="useDefaultPatterns">Whether to include the built-in default patterns. Default is <c>true</c>.</param>
    public RoleEscalationRule(string name = "RoleEscalation", SafetySeverity severity = SafetySeverity.Critical, string? description = null, IEnumerable<string>? additionalPatterns = null, bool useDefaultPatterns = true)
    {
        Name = name;
        _severity = severity;
        Description = description ?? "Detects role escalation and authority manipulation patterns.";

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
                return Task.FromResult(SafetyRuleResult.Block(Name, _severity, $"Potential role escalation detected matching pattern: {pattern}"));
            }

        return Task.FromResult(SafetyRuleResult.Allow(Name));
    }








    /// <inheritdoc />
    public string Name { get; }
}