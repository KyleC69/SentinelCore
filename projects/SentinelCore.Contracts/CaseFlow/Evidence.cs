// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Evidence.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.CaseFlow;





public sealed class Evidence
{
    public Case Case { get; set; } = null!;

    public int CaseRecordId { get; set; }

    public string ContentJson { get; set; } = null!;

    public int EvidenceId { get; set; }
    public int Id { get; set; }

    public string Provenance { get; set; } = null!;

    public string Source { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string Type { get; set; } = null!;
}