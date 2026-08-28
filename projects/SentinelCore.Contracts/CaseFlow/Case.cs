// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Case.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCore.Cfe;




namespace SentinelCore.Contracts;





public sealed class Case
{

    public Guid CaseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the collection of evidence items associated with the case.
    ///     To be considered evidence it must in some way support the hypothesis.
    /// </summary>
    /// <remarks>
    ///     Each evidence item represents a piece of information or data linked to the case,
    ///     including its provenance, source, type, and other metadata.
    /// </remarks>
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();

    public int Id { get; set; } // DB INTERNAL tracking

    /// <summary>
    ///     The ID of the original signal that initiated this case.
    /// </summary>
    /// One-To-One
    public int InitiatingSignal { get; set; }

    /// <summary>
    ///     The ID of the pattern memory (vector tracker) that was generated for this case.
    /// </summary>
    /// One-To-One
    public int? PatternMemoryId { get; set; }

    /// <summary>
    ///     The ID of the plan that was used to investigate this case. This is used to determine which plan to use when
    ///     generating a new
    ///     case from the same signal.
    /// </summary>
    /// One-To-One
    public int? PlanId { get; set; }

    /// <summary>
    ///     Additional signals that are INDICATORS or symptoms or side effects caused by this case's root cause.
    ///     These are additional items that may also indicate the same root cause.
    /// </summary>
    public ICollection<Signal> Signals { get; set; } = new List<Signal>();

    public CaseStatus Status { get; set; }

    public DateTime? UpdatedAt { get; set; }
}