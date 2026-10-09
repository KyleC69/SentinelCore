// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationObjective.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Represents an investigative objective capturing an assigned identifier, the initial signal summary, the signal
///     source and type, and the timestamp when the signal was observed.
/// </summary>
/// <remarks>
///     Identifier is assigned by the Case Flow Engine. Properties are immutable init-only DTO fields
///     intended for serialization; JSON property names are specified via attributes.
/// </remarks>
public sealed class InvestigationObjective
{


    [JsonPropertyName("id")]
    [Description("Unique identifier for the investigative objective. Assigned by the Case Flow Engine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; init; }


    [JsonPropertyName("signal")]
    [Description("The original signal that entered the system.")]
    public string? OriginalSignal { get; init; }



    [JsonPropertyName("InitialStatement")]
    [Description("The initial impression of the signal and what it might be an indication of.")]
    [JsonRequired]
    public required string SignalSummary { get; init; }


    [JsonPropertyName("subsystem")]
    [Description("possible subsystem sending the signal.")]
    [JsonRequired]
    public required string Source { get; init; }


    [Description("The type of source from which the signal originated. Event Log, Security Alert, Application Log, file log, Anomaly Detector etc.")]
    [JsonPropertyName("sourceType")]
    public required string SourceType { get; init; }



    [JsonPropertyName("timestap")]
    [Description("Time date of when the signal was received. if known")]
    public DateTime TimeStamp { get; init; }
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
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("Unique identifier for the piece of evidence.")]
    public Guid EvidenceId { get; init; }

    [JsonPropertyName("investigationId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Description("The identifier of the investigation this evidence supports.")]
    public required string InvestigationId { get; init; }

    [JsonPropertyName("metadata")]
    [Description("Additional structured metadata associated with the evidence.")]
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }

    [JsonPropertyName("observation")]
    [Description("The human-readable observation derived from the evidence.")]
    public required string Observation { get; init; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
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