// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         CaseEvidence.cs
// Author: Kyle L. Crowder
// Build Num:  101009



using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;




namespace SentinelCore.Contracts.CaseFlow;





public sealed class EvidenceEntity
{

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }

    [JsonPropertyName("caseId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Description("The identifier of the investigation this evidence supports.")]
    public int CaseId { get; set; }


    [JsonPropertyName("evidenceId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Description("Unique identifier for the piece of evidence. *Do Not Enter*")]
    public int EvidenceId { get; init; }


    [JsonPropertyName("collectorAgent")]
    [Description("The name of the agent that collected this evidence.")]
    public required string CollectorAgent { get; init; }

    [JsonPropertyName("sourcePath")]
    [Description("The specific path or location of the evidence in the source system.")]
    public string SourcePath { get; init; }

    [JsonPropertyName("sourceType")]
    [Description("The type of source from which the evidence was collected (e.g., EventLog, Registry).")]
    public string SourceType { get; init; }

    [JsonPropertyName("observation")]
    [Description("The human-readable observation derived from the evidence.")]
    public string Observation { get; init; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Description("The timestamp when the evidence was observed.")]
    public DateTimeOffset ObservedAt { get; init; }

    [JsonPropertyName("rawValue")]
    [Description("The date and time when the evidence was collected.")]
    public DateTime CreatedAt { get; set; }



    [JsonPropertyName("metadata")]
    [Description("Additional structured metadata associated with the evidence.")]
    public IReadOnlyDictionary<string, string> Metadata { get; init; }


    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) indicating the reliability of the evidence.")]
    public double Confidence { get; init; }









}