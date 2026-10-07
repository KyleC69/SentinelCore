// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationObjective.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.ComponentModel;
using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Represents a single investigative objective: a hypothesis about an observed signal together with metadata used to
///     validate or refute it.
/// </summary>
/// <remarks>
///     Instances are constructed with required values and are effectively immutable; the Id is generated at
///     construction. Confidence is a numeric score in the range 0.0–1.0. Use ObservablePredictions, KeyEntities,
///     SuggestedEvidenceDomains, and ContradictoryIndicators to guide evidence collection and hypothesis
///     validation.
/// </remarks>
public sealed class InvestigationObjective
{



    /// <summary>
    ///     Creates a new instance of <see cref="InvestigationObjective" /> with all required properties.
    /// </summary>
    [JsonConstructor]
    public InvestigationObjective()
    {

    }








    [JsonPropertyName("InitialStatement")]
    [Description("The initial impression of the signal and what it might be an indication of.")]
    public string HypothesisStatement { get; init; }


    [JsonPropertyName("id")]
    [Description("Unique identifier for the investigative objective. Assigned by the Case Flow Engine")]
    public string Id { get; }

    [JsonPropertyName("keyEntities")]
    [Description("possible properties or areas to interrogate the system and what to look for. (files, processes, registry keys) implicated by the hypothesis.")]
    public IList<string> KeyEntities { get; init; }


    [JsonPropertyName("signalSummary")]
    [Description("Concise summary of the original signal that triggered the investigation.")]
    public string SignalSummary { get; init; }

    [JsonPropertyName("suggestedEvidenceDomains")]
    [Description("Suggested evidence domains (e.g., Network, FileSystem) to query for validation.")]
    public IList<string> SuggestedEvidenceDomains { get; init; }
}





public sealed class Evidence
{

    [JsonPropertyName("collectorAgent")]
    [Description("The name of the agent that collected this evidence.")]
    public required string CollectorAgent { get; init; }

    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) indicating the reliability of the evidence.")]
    public double Confidence { get; init; }

    [JsonPropertyName("evidenceId")]
    [Description("Unique identifier for the piece of evidence.")]
    public Guid EvidenceId { get; init; }

    [JsonPropertyName("investigationId")]
    [Description("The identifier of the investigation this evidence supports.")]
    public required string InvestigationId { get; init; }

    [JsonPropertyName("metadata")]
    [Description("Additional structured metadata associated with the evidence.")]
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }

    [JsonPropertyName("observation")]
    [Description("The human-readable observation derived from the evidence.")]
    public required string Observation { get; init; }

    [JsonPropertyName("observedAt")]
    [Description("The timestamp when the evidence was observed.")]
    public DateTimeOffset ObservedAt { get; init; }

    [JsonPropertyName("rawValue")]
    [Description("The raw, unmodified data collected from the source.")]
    public required string RawValue { get; init; }

    [JsonPropertyName("sourcePath")]
    [Description("The specific path or location of the evidence in the source system.")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("sourceType")]
    [Description("The type of source from which the evidence was collected (e.g., EventLog, Registry).")]
    public required string SourceType { get; init; }
}





/*




*/





public sealed class InvestigationOutcome
{

    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) in the accuracy of the outcome.")]
    public double Confidence { get; init; }

    [JsonPropertyName("impact")]
    [Description("The assessed impact of the root cause on the system.")]
    public required string Impact { get; init; }

    [JsonPropertyName("investigationId")]
    [Description("The identifier of the investigation that produced this outcome.")]
    public Guid InvestigationId { get; init; }

    [JsonPropertyName("remediation")]
    [Description("Suggested remediation steps to prevent recurrence.")]
    public required string Remediation { get; init; }

    [JsonPropertyName("resolution")]
    [Description("The final resolution or conclusion of the investigation.")]
    public required string Resolution { get; init; }

    [JsonPropertyName("rootCause")]
    [Description("The determined root cause of the signal.")]
    public required string RootCause { get; init; }

    [JsonPropertyName("signal")]
    [Description("The original signal that was investigated.")]
    public required string Signal { get; init; }

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