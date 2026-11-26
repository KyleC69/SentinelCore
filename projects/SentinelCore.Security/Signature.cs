// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         Signature.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.AI;




namespace SentinelCore.Security;





/// <summary>
///     The signature stamp carried separately from provenance on every stamped message.
///     The stamp is stored on <see cref="ChatMessage" /> via the
///     <see cref="ChatMessage.AdditionalProperties" /> map under
///     <see cref="MessageSignature.AdditionalPropertiesKey" />. The HMAC-SHA256
///     signature covers the canonical payload: the provenance source pair, timestamp,
///     nonce, chat role, and message text.
/// </summary>
/// <param name="TimestampUtc">When the message was stamped (UTC).</param>
/// <param name="Nonce">A per-message random value that makes replay detectable.</param>
/// <param name="Signature">The HMAC-SHA256 signature over the canonical payload, Base64 encoded.</param>
public sealed record MessageSignature(DateTimeOffset TimestampUtc, string Nonce, string Signature)
{

    /// <summary>
    ///     The <see cref="ChatMessage.AdditionalProperties" /> key under which the
    ///     signature stamp is stored on a message.
    /// </summary>
    public const string AdditionalPropertiesKey = "signature";
}





/// <summary>
///     Extension methods for reading and stamping the signature stamp on
///     <see cref="ChatMessage" /> instances.
/// </summary>
public static class SignatureExtensions
{

    /// <summary>
    ///     Reads the signature stamp from a message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns>The stamp, or <c>null</c> when the message carries no signature stamp.</returns>
    public static MessageSignature? GetSignature(this ChatMessage message) =>
            message.GetStamp<MessageSignature>(MessageSignature.AdditionalPropertiesKey);








    /// <summary>
    ///     Stamps the message with the given signature, in place, and returns the same
    ///     instance. This is the same AdditionalProperties stamping mechanism used for
    ///     provenance, ChatMode and message-source attribution.
    /// </summary>
    /// <param name="message">The message to stamp.</param>
    /// <param name="signature">The signature stamp to attach.</param>
    /// <returns>The same message instance.</returns>
    public static ChatMessage WithSignature(this ChatMessage message, MessageSignature signature) =>
            message.WithStamp(MessageSignature.AdditionalPropertiesKey, signature);
}
