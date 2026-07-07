// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ReadOnlyRule.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Agents.Middleware;




namespace SentinelCoreLib.Agents.Rules;





/// <summary>
///     Blocks mutating tool calls unless explicitly allowed.
/// </summary>
public sealed class ReadOnlyRule : ISafetyRule
{

    /// <inheritdoc />
    public SafetyVerdict Evaluate(SafetyContext context)
    {
        if (context.FunctionCall is null)
        {
            return SafetyVerdict.Allowed(Name);
        }

        string toolName = context.FunctionCall.Name;
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return SafetyVerdict.Allowed(Name);
        }

        if (context.MutatingToolNames.Contains(toolName))
        {
            return SafetyVerdict.Blocked(Name, $"Mutating tool '{toolName}' is not permitted.");
        }

        return SafetyVerdict.Allowed(Name);
    }








    /// <inheritdoc />
    public string Name
    {
        get => "read_only";
    }
}