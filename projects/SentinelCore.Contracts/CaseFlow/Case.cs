// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Case.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Contracts.Cfe;




namespace SentinelCore.Contracts.CaseFlow;





public sealed class Case
{






    public Case()
    {
        CaseEvidence = new List<EvidenceEntity>();
        Status = CaseStatus.Initialized;
    }

    public int Id { get; set; } // DB INTERNAL tracking

    public int CaseId { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }


    /// <summary>
    ///     The ID of the original signal that initiated this case.
    /// </summary>
    /// One-To-One
    // The signal that started the case must be supplied when the case is created.
    public Signal InputSignal { get; set; }


    /// <summary>
    ///     The ID of the pattern memory (vector tracker) that was generated for this case.
    /// </summary>
    /// One-To-One

    /// <summary>
    /// Gets or sets the collection of evidence items associated with the case.
    /// </summary>
    /// <remarks>May be null or empty. Prefer initializing to an empty list to represent no evidence. Not
    /// guaranteed to be thread-safe.</remarks>
    /// One-To-Many
    // Initialise to an empty list to represent no evidence rather than leaving it null.
    public List<EvidenceEntity> CaseEvidence { get; set; } = new();


    public CaseStatus Status { get; set; }
    // Optional investigation objective; defaults to null.
    public InvestigationObjective? Objective { get; set; }
    public string CaseTranscript { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

}