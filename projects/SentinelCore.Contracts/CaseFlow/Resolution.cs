// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Resolution.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Cfe;





public sealed class Resolution
{
    public int CaseRecordId { get; set; }
    public int Id { get; set; }

    public string? Notes { get; set; }

    public string RawJsonContent { get; set; } = null!;

    public bool Verified { get; set; }
}