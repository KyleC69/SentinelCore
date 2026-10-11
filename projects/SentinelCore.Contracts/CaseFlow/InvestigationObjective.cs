// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationObjective.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;




namespace SentinelCore.Contracts.CaseFlow;





/// <summary>
/// Represents an investigative objective for a received signal, capturing the original signal, source and source type,
/// a concise signal summary, severity, known and unknown facts, investigation questions, relevant evidence sources, and
/// the timestamp when the signal was received.
/// </summary>
/// <remarks>Used by the Case Flow Engine to record and track investigation context and progress. The Id is
/// assigned by the Case Flow Engine and properties are init-only to preserve immutability.</remarks>
public sealed class InvestigationObjective
{


    [JsonPropertyName("id")]
    [Description("Unique identifier for the investigative objective. Assigned by the Case Flow Engine")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; init; }


    [JsonPropertyName("originalSignal")]
    [Description("The original signal that entered the system.")]
    [JsonRequired]
    public string? OriginalSignal { get; set; }



    [JsonPropertyName("signalSummary")]
    [Description("The initial impression of the signal and what it might be an indication of.")]
    [JsonRequired]
    public string SignalSummary { get; set; }


    [JsonPropertyName("source")]
    [Description("possible subsystem sending the signal.")]
    [JsonRequired]
    public string Source { get; init; }


    [Description("The type of source from which the signal originated. Event Log, Security Alert, Application Log, file log, Anomaly Detector etc.")]
    [JsonPropertyName("sourceType")]
    [JsonRequired]
    public string SourceType { get; init; }

    [JsonPropertyName("severity")]
    [Description("The severity of the signal. This could be low, medium, high, or critical.")]
    public string Severity { get; set; }

    [JsonPropertyName("knownFacts")]
    [Description("A list of known facts about the signal that are relevant to the investigation.")]
    public List<string> KnownFacts { get; set; } = [];

    [JsonPropertyName("unknownFacts")]
    [Description("A list of unknown facts about the signal that may be relevant to the investigation.")]
    public List<string> UnknownFacts { get; set; } = [];

    [JsonPropertyName("investigationQuestions")]
    [Description("A list of questions.")]
    public List<string> InvestigationQuestions { get; set; } = [];

    [JsonPropertyName("relevantEvidenceSources")]
    [Description("A list of evidence sources that may be relevant to the investigation.")]
    public List<string> RelevantEvidenceSources { get; set; } = [];


    [JsonPropertyName("timestamp")]
    [Description("Time date of when the signal was received. if known")]
    public DateTime TimeStamp { get; set; }
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