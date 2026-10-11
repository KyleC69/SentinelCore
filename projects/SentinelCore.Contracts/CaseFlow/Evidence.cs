// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Evidence.cs
// Author: Kyle L. Crowder
// Build Num:  100908



namespace SentinelCore.Contracts.CaseFlow;





public sealed class Evidence
{
    public int Id { get; set; }
    public int CaseId { get; set; } = 0;

    public string CaseRecordTranscript { get; set; }

    public string ContentJson { get; set; } = null!;

    public int EvidenceId { get; set; }


    public string Provenance { get; set; } = null!;

    public string Source { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string Type { get; set; } = null!;
}