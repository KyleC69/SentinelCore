// Solution: SentinelCore
// Project:   SentinelCore.Cfe
// File:         PatternMemory.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.Data.SqlTypes;

using SentinelCore.Contracts;




namespace SentinelCore.Cfe;





public sealed class PatternMemory
{
    public Case Case { get; set; } = null!;

    public int CaseId { get; set; }
    public int Id { get; set; }

    public int PatternId { get; set; }

    public SqlVector<float>? SignalEmbedding { get; set; }

    public string Summary { get; set; } = null!;

    public SqlVector<float>? SummaryEmbedding { get; set; }

    public DateTime Timestamp { get; set; }
}