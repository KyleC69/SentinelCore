// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Evidence.cs
// Author: Kyle L. Crowder
// Build Num:  110415



namespace SentinelCore.Contracts.CaseFlow;





// DTO (Data Transfer Object) for EvidenceEntity
public sealed class Evidence
{

    // One-to-Many relationship: A Case can have many Evidence items
    public Case Case { get; set; }
    public int CaseId { get; set; } = 0;

    public string CaseTranscript { get; set; }
    public string CollectorAgent { get; set; }
    public double Confidence { get; set; }

    public string ContentJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public int Id { get; set; }
    public string Observation { get; set; }
    public DateTime ObservedAt { get; set; }
    public string SourcePath { get; set; }
    public string SourceType { get; set; }



    public DateTime Timestamp { get; set; }

    public string Type { get; set; } = null!;
}