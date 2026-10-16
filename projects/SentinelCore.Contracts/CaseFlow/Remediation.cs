// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         Remediation.cs
// Author: Kyle L. Crowder
// Build Num:  101307



#nullable disable
using System.ComponentModel.DataAnnotations;





namespace SentinelCore.Contracts.CaseFlow;





public sealed partial class Remediation
{




    [Key] public int Id { get; set; }
    /// <summary>
    /// Foreign key to the CaseEntity table, linking this remediation to a specific case.
    /// </summary>
    public int CaseID { get; set; }
    /// <summary>
    /// The case data and metadata for the current case used by workflows and agents.
    /// </summary>
    /// <remarks>Required; must be non-null and initialized before use. Typically supplied by the caller to
    /// provide case context to processing components.</remarks>
    public required Case Case { get; set; }

    /// <summary>
    /// A brief summary of the remediation action.
    /// </summary>
    public string Summary { get; set; }
    /// <summary>
    /// The command or action to be executed as part of the remediation.
    /// </summary>
    public string Command { get; set; }
    /// <summary>
    /// The confidence level of the remediation action, typically a value between 0 and 1.
    /// </summary>  
    public double Confidence { get; set; }
    public string Impact { get; set; }
    public string Notes { get; set; }
}