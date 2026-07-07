// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         EvidenceItem.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application.Abstractions;





/// <summary>
///     A single piece of evidence attached to a case.
/// </summary>
public sealed class EvidenceItem
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="EvidenceItem" /> class.
    /// </summary>
    public EvidenceItem(string evidenceId, string type, string source, string contentJson, string provenance, DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(provenance);

        EvidenceId = evidenceId;
        Type = type;
        Source = source;
        ContentJson = contentJson;
        Provenance = provenance;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }








    /// <summary>
    ///     Gets the structured evidence content as JSON.
    /// </summary>
    public string ContentJson { get; }

    /// <summary>
    ///     Gets the evidence identifier.
    /// </summary>
    public string EvidenceId { get; }

    /// <summary>
    ///     Gets the provenance describing how the evidence was produced.
    /// </summary>
    public string Provenance { get; }

    /// <summary>
    ///     Gets the source of the evidence (e.g. tool name, agent name).
    /// </summary>
    public string Source { get; }

    /// <summary>
    ///     Gets the timestamp when the evidence was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Gets the evidence type (e.g. "registry", "wmi", "event_log").
    /// </summary>
    public string Type { get; }
}