// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEvaluationContext.cs
// Author: Kyle L. Crowder
// Build Num:  091112



namespace SentinelCore.Orchestrations.SafetyEngine;





/// <summary>
///     The context provided to a <see cref="ISafetyRule" /> during evaluation.
///     Contains the prompt messages and optional metadata.
/// </summary>
public sealed class SafetyEvaluationContext
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyEvaluationContext" />.
    /// </summary>
    /// <param name="messages">The chat messages that form the prompt being evaluated.</param>
    /// <param name="agentName">Optional agent name for agent-specific rules like rate limiting.</param>
    public SafetyEvaluationContext(IReadOnlyList<ChatMessage> messages, string? agentName = null)
    {
        Messages = messages;
        AgentName = agentName;
        Metadata = new Dictionary<string, object>();
    }








    /// <summary>
    ///     Optional agent name for rules that need agent-specific context (e.g., rate limiting).
    /// </summary>
    public string? AgentName { get; }

    /// <summary>
    ///     Convenience property: extracts the combined text content from all messages.
    /// </summary>
    public string CombinedText
    {
        get => string.Join("\n", Messages.Select(m => m.Text));
    }

    /// <summary>The chat messages that form the prompt being evaluated.</summary>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>Optional metadata dictionary for extensible context.</summary>
    public IDictionary<string, object> Metadata { get; init; }
}