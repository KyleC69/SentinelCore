// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorEnvelope.cs
// Author: Kyle L. Crowder
// Build Num:  101601



using SentinelCore.Data.DBEntities;




namespace SentinelCore.Orchestrations.Workflows.Executors;




/// <summary>
/// Helper class to unify executor input/output types
/// </summary>
public sealed class ExecutorEnvelope
{


    /// <summary>
    /// Case returned from new case
    /// </summary>
    public CaseEntity? Case { get; set; }


    /// <summary>
    /// Object sent to Magentic investigation group
    /// </summary>
    public Infrastructure.InvestigationObjective? Objective { get; set; }
    /// <summary>
    /// Original signal text input into the system
    /// </summary>
    public string? SignalText { get; set; }

}