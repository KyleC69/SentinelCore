// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatMessageExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Security;
using SentinelCore.Security.Sources;




namespace SentinelCore.Orchestrations.Models;





public static class ChatMessageExtensions
{

    /// <summary>
    ///     The key used in <see cref="ChatMessage.AdditionalProperties" /> to mark a message as chat history injected by a
    ///     history provider. This is an in-process routing marker, not provenance: it is never signed and never replaces
    ///     the provenance stamp written by <see cref="WithMessageSource" />.
    /// </summary>
    public const string ChatHistoryKey = "ChatHistory";



    /// <summary>
    ///     The key used in <see cref="ChatMessage.AdditionalProperties" /> to store the chat type a message was sent under.
    /// </summary>
    public const string ChatTypeKey = "ChatType";

    /// <summary>
    ///     The key used in <see cref="ChatMessage.AdditionalProperties" /> to store the safety action stamped for a turn.
    /// </summary>
    public const string SafetyResultKey = "SafetyResult";








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
        return new ChatMessage(ChatRole.Assistant, content);
    }








    public static ChatMessage AddUserMessage(this ChatMessage message, string content)
    {
        Throw.IfNullOrEmpty(content);
        return new(ChatRole.User, content);
    }








    /// <summary>
    ///     Gets the chat type the provided <see cref="ChatMessage" /> was sent under.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage" /> to read the chat type from.</param>
    /// <returns>
    ///     A <see cref="string" /> value with the chat type stamped by <see cref="WithChatType" />. Defaults to an empty
    ///     string if the message carries no chat type.
    /// </returns>
    public static string GetChatType(this ChatMessage message)
    {
        return message.GetMetaTagByKey(ChatTypeKey);
    }








    /// <summary>
    ///     Gets the registered source identifier stamped on the provided <see cref="ChatMessage" /> by
    ///     <see cref="WithMessageSource" />.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage" /> for which we need the source identifier.</param>
    /// <returns>
    ///     The <see cref="Guid" /> source identifier stamped on the message, or <see langword="null" /> when the message
    ///     carries no provenance stamp.
    /// </returns>
    public static Guid? GetMessageSourceId(this ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.GetProvenance()?.SourceId;
    }








    /// <summary>
    ///     Gets the source type stamped on the provided <see cref="ChatMessage" /> by <see cref="WithMessageSource" />.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage" /> for which we need the source type.</param>
    /// <returns>
    ///     A <see cref="SourceType" /> value indicating the source type of the <see cref="ChatMessage" />. Defaults to
    ///     <see cref="SourceType.ExternalSource" /> when the message carries no provenance stamp.
    /// </returns>
    public static SourceType GetMessageSourceType(this ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.GetProvenance()?.SourceType ?? SourceType.ExternalSource;
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
    ///     Determines whether the provided message was marked as chat history by a history provider.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns><see langword="true" /> when the message carries the <see cref="ChatHistoryKey" /> marker.</returns>
    public static bool IsChatHistoryMarked(this ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.AdditionalProperties?.ContainsKey(ChatHistoryKey) is true;
    }








    /// <summary>
    ///     Marks the provided message as chat history injected by a history provider, in place, and returns the same
    ///     instance.
    /// </summary>
    /// <param name="message">The message to mark. Must not be <see langword="null" />.</param>
    /// <returns>The same message instance, now marked.</returns>
    /// <remarks>
    ///     This is a routing marker for the history provider only. It deliberately does not use
    ///     <see cref="WithMessageSource" />: overwriting provenance on a signed message would invalidate its signature.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public static ChatMessage WithChatHistoryMarker(this ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.AdditionalProperties ??= new();
        message.AdditionalProperties[ChatHistoryKey] = true;
        return message;
    }








    /// <summary>
    ///     Stamps the provided message with the chat type it was sent under, stored in the
    ///     <see cref="ChatMessage.AdditionalProperties" /> under the <see cref="ChatTypeKey" /> key.
    /// </summary>
    /// <param name="message">The message to stamp. Must not be <see langword="null" />.</param>
    /// <param name="chatType">
    ///     The chat type name, e.g. <c>Chat</c> for environment questions or <c>Investigation</c> for
    ///     opening an investigation case. Must not be empty.
    /// </param>
    /// <returns>The stamped message.</returns>
    /// <remarks>
    ///     The message is stamped in place rather than cloned: the sender stamps a message it owns
    ///     before adding it to the conversation, mirroring how the safety engine stamps <c>SafetyScore</c>.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    ///     Thrown if <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    ///     Thrown if <paramref name="chatType" /> is null or whitespace.
    /// </exception>
    public static ChatMessage WithChatType(this ChatMessage message, string chatType)
    {
        ArgumentNullException.ThrowIfNull(message);
        Throw.IfNullOrWhitespace(chatType);

        message.AdditionalProperties ??= new();
        message.AdditionalProperties[ChatTypeKey] = chatType;
        return message;
    }








    /// <summary>
    ///     Stamps the provided message with the provenance of the given registered <see cref="MessageSource" />, in place,
    ///     and returns the same instance.
    /// </summary>
    /// <param name="message">The message to stamp. Must not be <see langword="null" />.</param>
    /// <param name="source">
    ///     A source from the fixed <see cref="SentinelSources" /> registry. The source pair cannot be supplied ad hoc.
    /// </param>
    /// <returns>The same message instance, now stamped.</returns>
    /// <remarks>
    ///     The stamp is the provenance stamp the security module signs and validates
    ///     (<see cref="MessageProvenance" />, key <see cref="MessageProvenance.AdditionalPropertiesKey" />), so a single
    ///     stamp serves both message-source attribution and signature validation. Stamping is in place, mirroring
    ///     <see cref="WithChatType" /> and the security module's provenance extensions.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="message" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    public static ChatMessage WithMessageSource(this ChatMessage message, MessageSource source)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(source);

        return message.WithProvenance(new MessageProvenance(source.SourceId, source.SourceType));
    }








    /// <summary>
    ///     Tags a prompt with the resulting safety action for the current turn, stored in the
    ///     <see cref="ChatMessage.AdditionalProperties" /> under the <see cref="SafetyResultKey" /> key.
    /// </summary>
    /// <param name="message">The message to tag. Must not be <see langword="null" />.</param>
    /// <param name="result">The safety action that applied to the turn.</param>
    /// <returns>The same message instance, now tagged.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public static ChatMessage WithSafetyResult(this ChatMessage message, string result)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.AdditionalProperties ??= new();
        message.AdditionalProperties[SafetyResultKey] = result;
        return message;
    }
}
