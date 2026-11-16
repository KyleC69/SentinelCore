// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatMessageExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using SentinelCore.CaseFlowEngine.Infrastructure;




namespace SentinelCore.Orchestrations.Models;





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
        return new ChatMessage(ChatRole.Assistant, content).WithAgentRequestMessageSource(new("Origin"), "SystemGenerated");
    }








    public static ChatMessage AddUserMessage(this ChatMessage message, string content)
    {
        Throw.IfNullOrEmpty(content);
        return new(ChatRole.User, content);
    }








    public static string GetMetaTagByKey(this ChatMessage message, string key)
    {
        if (message.AdditionalProperties != null && message.AdditionalProperties.TryGetValue(key, out object? value))
        {
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }














    /// <summary>
    ///     Tags a prompt with the resulting safety action for the current turn.
    /// </summary>
    public static ChatMessage WithSafetyResult(this ChatMessage message, string result)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.WithAgentRequestMessageSource(new("SafetyResult"), result);
    }






    /// <summary>
    /// Gets the source type of the provided <see cref="ChatMessage"/> in the context of messages passed into an agent run.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage"/> for which we need the source type.</param>
    /// <returns>An <see cref="MessageSourceType"/> value indicating the source type of the <see cref="ChatMessage"/>. Defaults to <see
    /// cref="MessageSourceType.External"/> if no explicit source is defined.</returns>
    public static MessageSourceType GetMessageSourceType(this ChatMessage message)
    {
        if (message.AdditionalProperties?.TryGetValue(AgentRequestMessageSourceAttribution.AdditionalPropertiesKey, out var attribution) is true
            && attribution is AgentRequestMessageSourceAttribution typedAttribution)
        {
            return typedAttribution.SourceType;
        }

        return MessageSourceType.External;
    }

    /// <summary>
    /// Gets the source id of the provided <see cref="ChatMessage"/> in the context of messages passed into an agent run.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage"/> for which we need the source id.</param>
    /// <returns>An <see cref="string"/> value indicating the source id of the <see cref="ChatMessage"/>. Defaults to <see langword="null"/>
    /// if no explicit source id is defined.</returns>
    public static string? GetMessageSourceId(this ChatMessage message)
    {
        if (message.AdditionalProperties?.TryGetValue(AgentRequestMessageSourceAttribution.AdditionalPropertiesKey, out var attribution) is true
            && attribution is AgentRequestMessageSourceAttribution typedAttribution)
        {
            return typedAttribution.SourceId;
        }

        return null;
    }







    /// <summary>
    /// Ensure that the provided message is tagged with the provided source type and source id in the context of a specific agent run.
    /// </summary>
    /// <param name="message">The message to tag.</param>
    /// <param name="sourceType">The source type to tag the message with.</param>
    /// <param name="sourceId">The source id to tag the message with.</param>
    /// <returns>The tagged message.</returns>
    /// <remarks>
    /// If the message is already tagged with the provided source type and source id, it is returned as is.
    /// Otherwise, a cloned message is returned with the appropriate tagging in the AdditionalProperties.
    /// </remarks>
    public static ChatMessage WithMessageSource(this ChatMessage message, MessageSourceType sourceType, string? sourceId = null)
    {
        if (message.AdditionalProperties != null
            // Check if the message was already tagged with the required source type and source id
            && message.AdditionalProperties.TryGetValue(AgentRequestMessageSourceAttribution.AdditionalPropertiesKey, out var messageSourceAttribution) && messageSourceAttribution is AgentRequestMessageSourceAttribution typedMessageSourceAttribution && typedMessageSourceAttribution.SourceType == sourceType && typedMessageSourceAttribution.SourceId == sourceId)
        {
            return message;
        }

        message = message.Clone();
        message.AdditionalProperties ??= new();
        message.AdditionalProperties[AgentRequestMessageSourceAttribution.AdditionalPropertiesKey] = new AgentRequestMessageSourceAttribution(sourceType, sourceId);
        return message;
    }

}
