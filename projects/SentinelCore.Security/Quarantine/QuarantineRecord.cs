// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         QuarantineRecord.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security.Quarantine;





/// <summary>
///     A message that failed provenance validation, persisted to the secure quarantine
///     folder. The record is a snapshot of the message's signed content — chat role,
///     text, and the provenance and signature stamps exactly as they arrived — together
///     with the event details. The full message object is not retained.
/// </summary>
/// <param name="QuarantinedAtUtc">When the message was quarantined (UTC).</param>
/// <param name="Reason">The validation failure that triggered quarantine.</param>
/// <param name="Detail">Human-readable details of the event.</param>
/// <param name="Role">The chat role of the offending message.</param>
/// <param name="Text">The text of the offending message, unchanged.</param>
/// <param name="SourceId">The provenance source identifier stamped on the message, when present.</param>
/// <param name="SourceType">The provenance source type stamped on the message, when present.</param>
/// <param name="StampTimestampUtc">The signature stamp timestamp, when present.</param>
/// <param name="Nonce">The signature stamp nonce, when present.</param>
/// <param name="Signature">The signature stamp signature, when present.</param>
public sealed record QuarantineRecord(DateTimeOffset QuarantinedAtUtc, ProvenanceFailure Reason, string Detail, string Role, string Text, Guid? SourceId, string? SourceType, DateTimeOffset? StampTimestampUtc, string? Nonce, string? Signature);