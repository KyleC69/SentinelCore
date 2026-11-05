// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Case.cs
// Author: Kyle L. Crowder
// Build Num:  110415



using SentinelCore.Contracts.Cfe;




namespace SentinelCore.Contracts.CaseFlow;





public sealed class Case
{



    public Case()
    {

        Status = CaseStatus.Initialized;
    }








    public string CaseTranscript { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();



    public int Id { get; set; } // DB INTERNAL tracking


    public InvestigationObjective? InvestigationObjective { get; set; }
    public InvestigationOutcome? InvestigationOutcome { get; set; }


    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public ICollection<Remediation> RemediationItems { get; set; } = new List<Remediation>();


    /// <summary>
    ///     The ID of the original signal that initiated this case.
    /// </summary>
    /// One-To-One
    // The signal that started the case must be supplied when the case is created.
    public Signal Signal { get; set; }

    public CaseStatus Status { get; set; }

    public DateTime? UpdatedAt { get; set; }
}