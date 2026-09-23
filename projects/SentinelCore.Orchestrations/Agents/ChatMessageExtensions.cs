#nullable enable

// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatMessageExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Abstractions;




namespace SentinelCore.Orchestrations.Agents;





public static class ChatMessageExtensions
{



    /// <summary>
    ///     Adds a new assistant message to the specified <see cref="ChatMessage" /> instance.
    /// </summary>
    /// <param name="message">
    ///     The existing <see cref="ChatMessage" /> instance to which the assistant message will be added.
    /// </param>
    /// <param name="content">
    ///     The content of the assistant message to be added. Must not be null or empty.
    /// </param>
    /// <returns>
    ///     A new <see cref="ChatMessage" /> instance representing the assistant message with the specified content.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    ///     Thrown if <paramref name="content" /> is null or empty.
    /// </exception>
    public static ChatMessage AddAssistantMessage(this ChatMessage message, string content)
    {
        Throw.IfNullOrEmpty(content);
        return new ChatMessage(ChatRole.Assistant, content).WithAgentRequestMessageSource(new AgentRequestMessageSourceType("Origin"), "SystemGenerated");
    }

    /// <summary>
    ///     Tags a prompt with the cumulative weighted safety score for the current turn.
    /// </summary>
    public static ChatMessage WithSafetyScore(this ChatMessage message, int score)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyScore"), score.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Tags a prompt with the resulting safety action for the current turn.
    /// </summary>
    public static ChatMessage WithSafetyResult(this ChatMessage message, string result)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyResult"), result);
    }

    /// <summary>
    ///     Applies both provenance tags required for safety-scored prompt messages.
    /// </summary>
    public static ChatMessage WithSafetyProvenance(this ChatMessage message, int score, string result)
    {
        ChatMessage taggedMessage = message;

        if (score > 0)
        {
            taggedMessage = taggedMessage.WithSafetyScore(score);
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            taggedMessage = taggedMessage.WithSafetyResult(result);
        }

        return taggedMessage;
    }
}
