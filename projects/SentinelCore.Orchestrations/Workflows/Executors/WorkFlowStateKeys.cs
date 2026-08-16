// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkFlowStateKeys.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Workflows.Executors;





/// <summary>
///     Keys used to save information to the context to be shared to other workflow steps.
///     NOTE: Context is scoped to "SharedState"
/// </summary>
public class WorkFlowStateKeys
{
    public const string CASE_ID = "CaseId";
    public const string PROMPT = "Prompt";
    public const string SIGNAL_HYPOTHESIS = "SignalHypothesis";
}