// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         Provenance.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.AI;

using SentinelCore.Security.Sources;




namespace SentinelCore.Security;





/// <summary>
///     The provenance security stamp carried by every message that enters Sentinel Core
///     through an approved entry point. Provenance defines only the source of the
///     message: a registered source identifier and its fixed source type, verifiable
///     against the hardcoded <see cref="SentinelSources" /> registry. The stamp is
///     stored on <see cref="ChatMessage" /> via the
///     <see cref="ChatMessage.AdditionalProperties" /> map under
///     <see cref="MessageProvenance.AdditionalPropertiesKey" /> — the same stamping
///     mechanism used for ChatMode and message-source attribution.
/// </summary>
/// <param name="SourceId">The registered source identifier (GUID) that produced the message.</param>
/// <param name="SourceType">The registered source type of the producing source.</param>
public sealed record MessageProvenance(Guid SourceId, SourceType SourceType)
{

    /// <summary>
    ///     The <see cref="ChatMessage.AdditionalProperties" /> key under which the
    ///     provenance stamp is stored on a message.
    /// </summary>
    public const string AdditionalPropertiesKey = "provenance";
}





/// <summary>
///     Reasons a message can fail provenance validation. Every failure is fail-closed:
///     the message is quarantined unchanged and never reaches a model.
/// </summary>
public enum ProvenanceFailure
{

    /// <summary>Validation succeeded.</summary>
    None,

    /// <summary>No provenance stamp was present on the message.</summary>
    MissingProvenance,

    /// <summary>The stamp could not be interpreted (wrong type or missing fields).</summary>
    MalformedProvenance,

    /// <summary>The source pair is not registered in the fixed source registry.</summary>
    UnknownSource,

    /// <summary>The stamp is outside the configured timestamp tolerance window.</summary>
    ExpiredTimestamp,

    /// <summary>The nonce was already consumed by an earlier validation pass.</summary>
    ReplayDetected,

    /// <summary>The HMAC signature does not verify against the message contents.</summary>
    InvalidSignature,

    /// <summary>The signing key could not be obtained; validation fails closed.</summary>
    KeyUnavailable,

    /// <summary>Validation itself threw; the message is quarantined as a precaution.</summary>
    InternalError

}





/// <summary>
///     The outcome of a provenance validation pass.
/// </summary>
public sealed class ProvenanceValidationResult
{

    private ProvenanceValidationResult(bool isValid, ProvenanceFailure failure, string detail)
    {
        IsValid = isValid;
        Failure = failure;
        Detail = detail;
    }








    /// <summary>Gets a human-readable description of the failure, or an empty string on success.</summary>
    public string Detail { get; }



    /// <summary>
    ///     Gets the failure reason when validation failed, or
    ///     <see cref="ProvenanceFailure.None" /> on success.
    /// </summary>
    public ProvenanceFailure Failure { get; }



    /// <summary>Gets a value indicating whether the message passed validation.</summary>
    public bool IsValid { get; }








    /// <summary>Creates a failed validation result.</summary>
    /// <param name="failure">The reason validation failed.</param>
    /// <param name="detail">A human-readable description of the failure.</param>
    /// <returns>A result marked as failed with the given reason and detail.</returns>
    public static ProvenanceValidationResult Failed(ProvenanceFailure failure, string detail) =>
            new(false, failure, detail ?? string.Empty);








    /// <summary>Creates a successful validation result.</summary>
    /// <returns>A result marked as valid.</returns>
    public static ProvenanceValidationResult Success() => new(true, ProvenanceFailure.None, string.Empty);
}





/// <summary>
///     Extension methods for reading and stamping message provenance on
///     <see cref="ChatMessage" /> instances.
/// </summary>
public static class ProvenanceExtensions
{

    /// <summary>
    ///     Reads the provenance stamp from a message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns>The stamp, or <c>null</c> when the message carries no stamp.</returns>
    public static MessageProvenance? GetProvenance(this ChatMessage message) =>
            message.GetStamp<MessageProvenance>(MessageProvenance.AdditionalPropertiesKey);








    /// <summary>
    ///     Stamps the message with the given provenance, in place, and returns the same
    ///     instance. This is the same AdditionalProperties stamping mechanism used for
    ///     ChatMode and message-source attribution.
    /// </summary>
    /// <param name="message">The message to stamp.</param>
    /// <param name="provenance">The provenance stamp to attach.</param>
    /// <returns>The same message instance.</returns>
    public static ChatMessage WithProvenance(this ChatMessage message, MessageProvenance provenance) =>
            message.WithStamp(MessageProvenance.AdditionalPropertiesKey, provenance);
}
