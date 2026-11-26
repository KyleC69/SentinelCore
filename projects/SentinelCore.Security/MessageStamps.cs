// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         MessageStamps.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.AI;




namespace SentinelCore.Security;

/// <summary>
///     Primitive read/write access to the <see cref="ChatMessage.AdditionalProperties" />
///     stamp map shared by every Sentinel stamp type (provenance, signature, and
///     message attribution). Per-type extension methods delegate to these members so
///     each stamp keeps one well-known key without duplicating lookup, type-check, and
///     map-initialization logic.
/// </summary>
public static class MessageStamps
{

    /// <summary>
    ///     Reads the stamp stored under the given AdditionalProperties key.
    /// </summary>
    /// <typeparam name="TStamp">The stamp type expected under the key.</typeparam>
    /// <param name="message">The message to inspect.</param>
    /// <param name="key">The AdditionalProperties key under which the stamp is stored.</param>
    /// <returns>The stamp, or <see langword="null" /> when the message carries no stamp of that type under the key.</returns>
    public static TStamp? GetStamp<TStamp>(this ChatMessage message, string key) where TStamp : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return message.AdditionalProperties is not null && message.AdditionalProperties.TryGetValue(key, out object? value) && value is TStamp stamp ? stamp : null;
    }





    /// <summary>
    ///     Attempts to read the stamp stored under the given AdditionalProperties key.
    /// </summary>
    /// <typeparam name="TStamp">The stamp type expected under the key.</typeparam>
    /// <param name="message">The message to inspect.</param>
    /// <param name="key">The AdditionalProperties key under which the stamp is stored.</param>
    /// <param name="stamp">The stamp, when present and of the expected type.</param>
    /// <returns><see langword="true" /> when the stamp is present and of the expected type.</returns>
    public static bool TryGetStamp<TStamp>(this ChatMessage message, string key, [NotNullWhen(true)] out TStamp? stamp) where TStamp : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(key);

        stamp = GetStamp<TStamp>(message, key);
        return stamp is not null;
    }





    /// <summary>
    ///     Stamps the message under the given AdditionalProperties key, in place, and
    ///     returns the same instance. This is the same stamping mechanism used by every
    ///     Sentinel stamp type.
    /// </summary>
    /// <typeparam name="TStamp">The stamp type to store.</typeparam>
    /// <param name="message">The message to stamp.</param>
    /// <param name="key">The AdditionalProperties key under which the stamp is stored.</param>
    /// <param name="stamp">The stamp to attach.</param>
    /// <returns>The same message instance.</returns>
    public static ChatMessage WithStamp<TStamp>(this ChatMessage message, string key, TStamp stamp) where TStamp : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(stamp);

        (message.AdditionalProperties ??= new())[key] = stamp;
        return message;
    }
}
