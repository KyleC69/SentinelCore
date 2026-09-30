// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationHypothesis.cs
// Author: Kyle L. Crowder
// Build Num:  093006



using System.ComponentModel;
using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
/// Represents a candidate investigative hypothesis that explains an observed signal, encapsulating a concise hypothesis
/// statement, numeric confidence, supporting reasoning, predicted observable outcomes, implicated entities, suggested
/// evidence domains, and any known contradictory indicators.
/// </summary>
/// <remarks>Immutable data transfer object intended for exchange between investigative agents and workflows. Use
/// concise, evidence‑linked fields; treat Confidence as a probability in the range 0.0 to 1.0. ObservablePredictions
/// and SuggestedEvidenceDomains are intended to guide evidence collection and validation.</remarks>
public sealed class InvestigationObjective
{
    [JsonPropertyName("id")]
    [Description("Unique identifier for the investigative hypothesis. Assigned by the Case Flow Engine")]
    public required string Id { get; init; }

    [JsonPropertyName("signalSummary")]
    [Description("Concise summary of the original signal that triggered the investigation.")]
    public required string SignalSummary { get; init; }

    [JsonPropertyName("hypothesisStatement")]
    [Description("The primary hypothesis statement explaining the observed signal.")]
    public required string HypothesisStatement { get; init; }

    [JsonPropertyName("confidence")]
    [Description("Numeric confidence score (0.0 to 1.0) indicating the probability that the hypothesis is correct.")]
    public double Confidence { get; init; }

    [JsonPropertyName("reasoningSummary")]
    [Description("Summary of the reasoning and logic used to derive the hypothesis.")]
    public required string ReasoningSummary { get; init; }

    [JsonPropertyName("observablePredictions")]
    [Description("Predicted observable outcomes that would validate the hypothesis.")]
    public required IReadOnlyList<string> ObservablePredictions { get; init; }

    [JsonPropertyName("keyEntities")]
    [Description("Key system entities (files, processes, registry keys) implicated by the hypothesis.")]
    public required IReadOnlyList<string> KeyEntities { get; init; }

    [JsonPropertyName("suggestedEvidenceDomains")]
    [Description("Suggested evidence domains (e.g., Network, FileSystem) to query for validation.")]
    public required IReadOnlyList<string> SuggestedEvidenceDomains { get; init; }

    [JsonPropertyName("contradictoryIndicators")]
    [Description("Known indicators or evidence that contradict this hypothesis.")]
    public required IReadOnlyList<string> ContradictoryIndicators { get; init; }
}




public sealed class Evidence
{
    [JsonPropertyName("evidenceId")]
    [Description("Unique identifier for the piece of evidence.")]
    public Guid EvidenceId { get; init; }

    [JsonPropertyName("investigationId")]
    [Description("The identifier of the investigation this evidence supports.")]
    public required string InvestigationId { get; init; }

    [JsonPropertyName("sourceType")]
    [Description("The type of source from which the evidence was collected (e.g., EventLog, Registry).")]
    public required string SourceType { get; init; }

    [JsonPropertyName("sourcePath")]
    [Description("The specific path or location of the evidence in the source system.")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("observation")]
    [Description("The human-readable observation derived from the evidence.")]
    public required string Observation { get; init; }

    [JsonPropertyName("rawValue")]
    [Description("The raw, unmodified data collected from the source.")]
    public required string RawValue { get; init; }

    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) indicating the reliability of the evidence.")]
    public double Confidence { get; init; }

    [JsonPropertyName("observedAt")]
    [Description("The timestamp when the evidence was observed.")]
    public DateTimeOffset ObservedAt { get; init; }

    [JsonPropertyName("metadata")]
    [Description("Additional structured metadata associated with the evidence.")]
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }

    [JsonPropertyName("collectorAgent")]
    [Description("The name of the agent that collected this evidence.")]
    public required string CollectorAgent { get; init; }
}

/*




*/

public sealed class InvestigationOutcome
{
    [JsonPropertyName("investigationId")]
    [Description("The identifier of the investigation that produced this outcome.")]
    public Guid InvestigationId { get; init; }

    [JsonPropertyName("signal")]
    [Description("The original signal that was investigated.")]
    public required string Signal { get; init; }

    [JsonPropertyName("rootCause")]
    [Description("The determined root cause of the signal.")]
    public required string RootCause { get; init; }

    [JsonPropertyName("impact")]
    [Description("The assessed impact of the root cause on the system.")]
    public required string Impact { get; init; }

    [JsonPropertyName("resolution")]
    [Description("The final resolution or conclusion of the investigation.")]
    public required string Resolution { get; init; }

    [JsonPropertyName("remediation")]
    [Description("Suggested remediation steps to prevent recurrence.")]
    public required string Remediation { get; init; }

    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) in the accuracy of the outcome.")]
    public double Confidence { get; init; }

    [JsonPropertyName("supportingFactors")]
    [Description("Key factors and evidence that support this outcome.")]
    public required List<string> SupportingFactors { get; init; }
}
/*
"""
Signal:
Windows Update Failure

Root Cause:
Group policy disabled Windows Update functionality.

Impact:
Update scanning and installation prevented.

Supporting Factors:
NoAutoUpdate=1
wuauserv stopped
Policy key present

Resolution:
Removed policy and restarted update service.

Remediation:
Review GPO assignment and verify intended management policy.
"""
*/