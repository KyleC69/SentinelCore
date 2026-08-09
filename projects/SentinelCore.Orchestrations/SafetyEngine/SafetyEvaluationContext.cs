// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEvaluationContext.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.SafetyEngine;





/// <summary>
///     The context provided to a <see cref="ISafetyRule" /> during evaluation.
///     Contains the prompt messages and optional metadata.
/// </summary>
public sealed class SafetyEvaluationContext
{

    public SafetyEvaluationContext(IReadOnlyList<ChatMessage> messages)
    {
        Messages = messages;
        Metadata = new Dictionary<string, object>();
    }








    /// <summary>
    ///     Convenience property: extracts the combined text content from all messages.
    /// </summary>
    public string CombinedText
    {
        get => string.Join("\n", Messages.Where(m => m.Text is not null).Select(m => m.Text));
    }

    /// <summary>The chat messages that form the prompt being evaluated.</summary>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>Optional metadata dictionary for extensible context.</summary>
    public IDictionary<string, object> Metadata { get; init; }
}