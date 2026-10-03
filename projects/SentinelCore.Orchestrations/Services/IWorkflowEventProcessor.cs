// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IWorkflowEventProcessor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



namespace SentinelCore.Orchestrations.Services;





/// <summary>
///     Defines the contract for processing and formatting workflow events.
///     This decouples event handling logic from the base workflow class.
/// </summary>
public interface IWorkflowEventProcessor
{
    /// <summary>
    ///     Processes a single workflow event, handling formatting and reporting.
    /// </summary>
    /// <param name="evt">The workflow event to process.</param>
    /// <returns>A string containing the formatted event details, or an empty string if none.</returns>
    string ProcessEvent(WorkflowEvent evt);








    /// <summary>
    ///     Clears all accumulated streaming response chunks.
    /// </summary>
    void ResetEventAccumulators();
}