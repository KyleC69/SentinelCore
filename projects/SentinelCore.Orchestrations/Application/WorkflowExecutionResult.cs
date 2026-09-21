// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowExecutionResult.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Diagnostics.CodeAnalysis;




namespace SentinelCore.Orchestrations.Application;





// ─────────────────────────────────────────────────────────────────────────────────
//  Result & Event Types
// ─────────────────────────────────────────────────────────────────────────────────





/// <summary>
///     Encapsulates the result of a workflow execution, including the final
///     output messages and a structured log of all captured events.
///     Constructed exclusively by the workflow execution engine — the internal
///     constructor is the only creation path, so callers cannot fabricate results.
/// </summary>
public sealed class WorkflowExecutionResult
{
    internal WorkflowExecutionResult(List<ChatMessage>? outputMessages, [NotNull] List<WorkflowEventEntry> eventLog)
    {
        OutputMessages = outputMessages;
        EventLog = eventLog;
    }





    /// <summary>
    ///     A chronological log of all workflow events captured during execution.
    /// </summary>
    public IReadOnlyList<WorkflowEventEntry> EventLog { get; }

    /// <summary>
    ///     Whether the workflow produced output messages.
    /// </summary>
    public bool HasOutput
    {
        get => OutputMessages is not null && OutputMessages.Count > 0;
    }

    /// <summary>
    ///     Convenience accessor for the last assistant message text, or <c>null</c>.
    /// </summary>
    public string? LastAssistantMessage
    {
        get
        {
            if (OutputMessages is null) return null;

            for (int i = OutputMessages.Count - 1; i >= 0; i--)
                if (OutputMessages[i].Role == ChatRole.Assistant && !string.IsNullOrEmpty(OutputMessages[i].Text))
                    return OutputMessages[i].Text;

            return null;
        }
    }

    /// <summary>
    ///     The final output messages from the workflow, if any were produced.
    /// </summary>
    public List<ChatMessage>? OutputMessages { get; }
}
