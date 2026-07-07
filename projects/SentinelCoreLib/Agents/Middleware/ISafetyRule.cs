// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ISafetyRule.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Agents.Middleware;





/// <summary>
///     Evaluates a safety rule against a chat message, tool call, or tool result.
/// </summary>
public interface ISafetyRule
{
    /// <summary>
    ///     Gets the rule name.
    /// </summary>
    string Name { get; }








    /// <summary>
    ///     Evaluates the provided context and returns a verdict.
    /// </summary>
    /// <param name="context">The safety evaluation context.</param>
    /// <returns>A safety verdict.</returns>
    SafetyVerdict Evaluate(SafetyContext context);
}





/// <summary>
///     Context provided to safety rules for evaluation.
/// </summary>
public sealed class SafetyContext
{
    /// <summary>
    ///     The case identifier associated with the request, if any.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    ///     The function call content being evaluated, if any.
    /// </summary>
    public FunctionCallContent? FunctionCall { get; init; }

    /// <summary>
    ///     The function result content being evaluated, if any.
    /// </summary>
    public FunctionResultContent? FunctionResult { get; init; }

    /// <summary>
    ///     The chat message being evaluated, if any.
    /// </summary>
    public ChatMessage? Message { get; init; }

    /// <summary>
    ///     The set of tool names that are allowed to mutate state.
    /// </summary>
    public IReadOnlySet<string> MutatingToolNames { get; init; } = new HashSet<string>();

    /// <summary>
    ///     The registered tool names available for validation.
    /// </summary>
    public IReadOnlySet<string> RegisteredToolNames { get; init; } = new HashSet<string>();
}





/// <summary>
///     Verdict produced by a safety rule.
/// </summary>
public sealed class SafetyVerdict
{

    private SafetyVerdict(bool isAllowed, string reason, string ruleName)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        RuleName = ruleName;
    }








    /// <summary>
    ///     Gets a value indicating whether the operation is allowed.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    ///     Gets the reason for the verdict.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    ///     Gets the rule name that produced the verdict.
    /// </summary>
    public string RuleName { get; }








    /// <summary>
    ///     Creates an allowed verdict.
    /// </summary>
    public static SafetyVerdict Allowed(string ruleName, string reason = "") => new(true, reason, ruleName);








    /// <summary>
    ///     Creates a blocked verdict.
    /// </summary>
    public static SafetyVerdict Blocked(string ruleName, string reason) => new(false, reason, ruleName);
}





/// <summary>
///     Coordinates safety rule evaluation and logs safety events.
/// </summary>
public interface ISafetyMiddleware
{
    /// <summary>
    ///     Evaluates the provided context against all registered rules.
    /// </summary>
    /// <param name="context">The safety context.</param>
    /// <returns>The first blocking verdict, or an allowed verdict if all rules pass.</returns>
    SafetyVerdict Evaluate(SafetyContext context);
}