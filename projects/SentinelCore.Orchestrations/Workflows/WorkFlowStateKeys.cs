// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkFlowStateKeys.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Keys used to save information to the context to be shared to other workflow steps.
///     NOTE: Context is scoped to "SharedState"
/// </summary>
public class WorkFlowStateKeys
{
    public const string CASE_ENTITY = "CaseEntity";
    public const string CASE_ID = "CaseId";

    /// <summary>
    ///     The validated user prompt for the current workflow run. Canonical payload type:
    ///     <see cref="Microsoft.Extensions.AI.ChatMessage" /> — the original message as it
    ///     passed the safety gate. Writers queue a ChatMessage; readers use
    ///     <c>ReadStateAsync&lt;ChatMessage&gt;</c>. Do not queue string variants of this key.
    /// </summary>
    public const string PROMPT = "Prompt";

    public const string SIGNAL_OBJECTIVE = "Objective";
}