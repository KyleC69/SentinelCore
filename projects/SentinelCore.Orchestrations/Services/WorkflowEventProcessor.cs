// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowEventProcessor.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.Linq;
using System.Text;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;

using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Services;





/// <summary>
///     Concrete implementation of IWorkflowEventProcessor.
///     Handles the formatting and reporting of all workflow events,
///     including streaming accumulation and flushing.
/// </summary>
public class WorkflowEventProcessor : IWorkflowEventProcessor
{
    private readonly object _lock = new();
    private readonly Stack<StringBuilder> _pool = new();
    private readonly ISystemReporter _reporter;
    private readonly Dictionary<string, StringBuilder> _responseAccumulators = new(StringComparer.Ordinal);








    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkflowEventProcessor" /> class.
    /// </summary>
    /// <param name="reporter">The reporter used for workflow event diagnostics and errors.</param>
    public WorkflowEventProcessor(ISystemReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }








    /// <summary>
    ///     Processes a single workflow event, handling formatting and reporting.
    /// </summary>
    /// <param name="evt">The workflow event to process.</param>
    /// <returns>A string containing the formatted event details, or an empty string if none.</returns>
    public string ProcessEvent(WorkflowEvent evt)
    {
        // Validate the event
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        // Buffer streaming update chunks; they are reported as part of the complete message.
        if (evt is AgentResponseUpdateEvent updateEvent)
        {
            string accumulatorKey = BuildAccumulatorKey(updateEvent.ExecutorId, updateEvent.Update.ResponseId, updateEvent.Update.MessageId);
            AccumulateUpdate(accumulatorKey, updateEvent.Update.Text);
            return string.Empty; // Don't clutter UI with "buffered" messages.
        }



        // Process event based on its type
        string? eventDetails = GetEventDetails(evt);

        if (eventDetails is null) return string.Empty;

        // Route error events to ReportError, others to ReportInfo
        if (evt is SubworkflowErrorEvent or ExecutorFailedEvent or WorkflowErrorEvent)
        {
            _reporter.ReportError(eventDetails, (evt as WorkflowErrorEvent)?.Exception ?? (evt as SubworkflowErrorEvent)?.Exception);
        }
        else if (evt is not WorkflowOutputEvent)
        {
            _reporter.ReportInfo(eventDetails);
        }

        // Return the fully assembled message for display in the UI
        if (evt is WorkflowOutputEvent or AgentResponseEvent or ExecutorCompletedEvent)
        {
            return eventDetails;
        }

        return string.Empty;
    }








    /// <summary>
    ///     Clears all accumulated streaming response chunks.
    /// </summary>
    public void ResetEventAccumulators()
    {
        lock (_lock)
        {
            foreach (StringBuilder sb in _responseAccumulators.Values) ReturnStringBuilder(sb);
            _responseAccumulators.Clear();
        }
    }








    // --- Private Helper Methods ---








    /// <summary>
    ///     Buffers a streamed response chunk under its response-stream key.
    /// </summary>
    /// <param name="accumulatorKey">Composite key that identifies a single response stream.</param>
    /// <param name="chunk">The streamed text chunk to append.</param>
    private void AccumulateUpdate(string accumulatorKey, string chunk)
    {
        lock (_lock)
        {
            if (!_responseAccumulators.TryGetValue(accumulatorKey, out StringBuilder? sb))
            {
                sb = GetStringBuilder();
                _responseAccumulators[accumulatorKey] = sb;
            }

            sb.Append(chunk);
        }
    }








    /// <summary>
    ///     Flushes all buffered chunks associated with the specified executor.
    /// </summary>
    /// <param name="executorId">The executor identifier whose buffered chunks should be flushed.</param>
    /// <returns>Concatenated buffered output for the executor, if any; otherwise an empty string.</returns>
    private string FlushAccumulatedResponsesForExecutor(string executorId)
    {
        List<string> keysToFlush;
        lock (_lock)
        {
            keysToFlush = _responseAccumulators.Keys
                .Where(key => key.StartsWith(executorId + ":", StringComparison.Ordinal))
                .ToList();
        }

        if (keysToFlush.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder combined = GetStringBuilder();
        try
        {
            foreach (string key in keysToFlush)
            {
                StringBuilder? sb;
                lock (_lock)
                {
                    if (!_responseAccumulators.Remove(key, out sb))
                    {
                        continue;
                    }
                }

                if (combined.Length > 0)
                {
                    combined.AppendLine();
                }

                combined.Append(sb.ToString());
                ReturnStringBuilder(sb);
            }

            return combined.ToString();
        }
        finally
        {
            ReturnStringBuilder(combined);
        }
    }








    /// <summary>
    ///     Flushes all buffered chunks across all executors.
    /// </summary>
    /// <returns>Concatenated buffered output, if any; otherwise an empty string.</returns>
    private string FlushAllAccumulatedResponses()
    {
        List<string> keysToFlush;
        lock (_lock)
        {
            keysToFlush = _responseAccumulators.Keys.ToList();
        }

        if (keysToFlush.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder combined = GetStringBuilder();
        try
        {
            foreach (string key in keysToFlush)
            {
                StringBuilder? sb;
                lock (_lock)
                {
                    if (!_responseAccumulators.Remove(key, out sb))
                    {
                        continue;
                    }
                }

                if (combined.Length > 0)
                {
                    combined.AppendLine();
                }

                combined.Append(sb.ToString());
                ReturnStringBuilder(sb);
            }

            return combined.ToString();
        }
        finally
        {
            ReturnStringBuilder(combined);
        }
    }








    /// <summary>
    ///     Builds a stable stream key for buffering updates from the same executor response.
    /// </summary>
    /// <param name="executorId">The executor that emitted the update.</param>
    /// <param name="responseId">Optional provider response identifier.</param>
    /// <param name="messageId">Optional provider message identifier.</param>
    /// <returns>A composite key used by the response accumulator.</returns>
    private static string BuildAccumulatorKey(string executorId, string? responseId, string? messageId)
    {
        string streamId = !string.IsNullOrWhiteSpace(responseId)
            ? responseId
            : !string.IsNullOrWhiteSpace(messageId)
                ? messageId
                : executorId;

        return $"{executorId}:{streamId}";
    }








    /// <summary>
    ///     Formats a completed agent response and merges it with any buffered streamed chunks.
    /// </summary>
    /// <param name="evt">The completed agent response event.</param>
    /// <returns>Formatted response text for UI display, or null when no output exists.</returns>
    private string? FormatAgentResponseEvent(AgentResponseEvent evt)
    {
        string accumulated = FlushAccumulatedResponsesForExecutor(evt.ExecutorId);
        string text = evt.Response.Text;

        // Determine the most complete version of the output.
        string fullOutput;
        if (string.IsNullOrEmpty(accumulated))
        {
            fullOutput = text;
        }
        else if (string.IsNullOrEmpty(text))
        {
            fullOutput = accumulated;
        }
        else if (text.Contains(accumulated, StringComparison.Ordinal))
        {
            fullOutput = text;
        }
        else if (accumulated.Contains(text, StringComparison.Ordinal))
        {
            fullOutput = accumulated;
        }
        else
        {
            // Combine them if they aren't subsets.
            fullOutput = accumulated + text;
        }

        if (string.IsNullOrWhiteSpace(fullOutput))
        {
            return null;
        }

        return $"Agent {evt.ExecutorId} response: {fullOutput}";
    }








    private string? FormatExecutorCompletedEvent(ExecutorCompletedEvent evt)
    {
        string accumulated = FlushAccumulatedResponsesForExecutor(evt.ExecutorId);
        if (string.IsNullOrEmpty(accumulated))
        {
            return null; // Return null so it's not printed if no output was pending
        }

        return $"Agent {evt.ExecutorId} (completed): {accumulated}";
    }








    private string? FormatExecutorEvent(ExecutorEvent evt)
    {
        if (evt == null)
        {
            throw new ArgumentNullException(nameof(evt));
        }



        StringBuilder sb = GetStringBuilder();
        try
        {
            sb.AppendLine($"Executor Event: {evt.ExecutorId}");
            sb.AppendLine($"Timestamp: {evt.Data}");

            return sb.ToString();
        }
        finally
        {
            ReturnStringBuilder(sb);
        }
    }








    private string FormatExecutorFailedEvent(ExecutorFailedEvent evt)
    {
        // evt.Data may be null; guard against NRE.
        string message = evt.Data?.Message ?? "(no error message)";
        return $"Executor failed: {evt.ExecutorId}, Error: {message}";
    }








    private string FormatExecutorInvokedEvent(ExecutorInvokedEvent evt)
    {
        return $"Executor invoked: {evt.ExecutorId}";
    }








    private string? FormatMagenticPlanCreatedEvent(MagenticPlanCreatedEvent evt)
    {
        if (evt == null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        StringBuilder sb = GetStringBuilder();
        try
        {

            sb.AppendLine("Magentic Plan Created Event:");
            sb.AppendLine($"Details: {evt.FullTaskLedger}");
            sb.AppendLine($"Created By: {evt.Data}");

            return sb.ToString();
        }
        finally
        {
            ReturnStringBuilder(sb);
        }
    }








    private string? FormatOrchestratorEvent(MagenticOrchestratorEvent evt)
    {
        if (evt == null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        return $"Orchestrator Event: {evt.Data}";
    }








    private string? FormatProgressLedger(MagenticProgressLedgerUpdatedEvent magenticProgressLedgerUpdatedEvent)
    {
        if (magenticProgressLedgerUpdatedEvent == null)
        {
            throw new ArgumentNullException(nameof(magenticProgressLedgerUpdatedEvent));
        }

        StringBuilder stringBuilder = GetStringBuilder();
        try
        {
            stringBuilder.AppendLine("Progress Ledger Updated:");
            stringBuilder.AppendLine($"Timestamp: {magenticProgressLedgerUpdatedEvent.ProgressLedger}");
            stringBuilder.AppendLine($"Executor ID: {magenticProgressLedgerUpdatedEvent.Data}");

            return stringBuilder.ToString();
        }
        finally
        {
            ReturnStringBuilder(stringBuilder);
        }
    }








    private string? FormatReplannedEvent(MagenticReplannedEvent evt)
    {
        if (evt == null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        StringBuilder stringBuilder = GetStringBuilder();
        try
        {
            stringBuilder.AppendLine("Magentic Replanned Event:");
            stringBuilder.AppendLine($"- Plan ID: {evt.FullTaskLedger}");
            stringBuilder.AppendLine($"- Timestamp: {evt.Data}");
            stringBuilder.AppendLine($"- Reason: {evt.ToString()}");

            return stringBuilder.ToString();
        }
        finally
        {
            ReturnStringBuilder(stringBuilder);
        }
    }








    private string FormatRequestInfoEvent(RequestInfoEvent evt)
    {
        return $"Request info: {evt.Request.RequestId} {evt.Request.Data}";
    }








    private string FormatSubWorkflowErrorEvent(SubworkflowErrorEvent subworkflowError)
    {
        // No meaningful error string is currently available; return an empty string to avoid null.
        return $"SubWorkflow error: {subworkflowError.SubworkflowId}, Error: {subworkflowError.Data}";
    }








    private string FormatSuperStepCompletedEvent(SuperStepCompletedEvent evt)
    {
        _reporter.ReportDebug($"SuperStep completed: [{evt.StepNumber}] CompletionInfo:  {evt.CompletionInfo}");
        _reporter.ReportDebug($"Activated: {evt.CompletionInfo.ActivatedExecutors} Instatiated: {evt.CompletionInfo.InstantiatedExecutors} HasPending-Msgs {evt.CompletionInfo.HasPendingMessages} requests {evt.CompletionInfo.HasPendingRequests} State Updated {evt.CompletionInfo.StateUpdated} ");
        return $"Activated: {evt.CompletionInfo.ActivatedExecutors} Instatiated: {evt.CompletionInfo.InstantiatedExecutors} HasPending-Msgs {evt.CompletionInfo.HasPendingMessages} requests {evt.CompletionInfo.HasPendingRequests} State Updated {evt.CompletionInfo.StateUpdated} ";
        return $"Superstep completed: {evt.CompletionInfo}, data: {evt.Data}";
    }








    private string? FormatSuperStepEvent(SuperStepEvent superStepEvent)
    {
        if (superStepEvent == null)
        {
            throw new ArgumentNullException(nameof(superStepEvent));
        }

        StringBuilder stringBuilder = GetStringBuilder();
        try
        {
            stringBuilder.AppendLine($"SuperStep Event StepNum: {superStepEvent.StepNumber}");
            stringBuilder.AppendLine($"Data: {superStepEvent.Data}");
            stringBuilder.AppendLine($"Details: {superStepEvent.ToString()}");

            return stringBuilder.ToString();
        }
        finally
        {
            ReturnStringBuilder(stringBuilder);
        }
    }








    private string FormatSuperStepStartedEvent(SuperStepStartedEvent evt)
    {
        return $"Superstep started: {evt.StepNumber}";
    }








    private string FormatWorkflowErrorEvent(WorkflowErrorEvent evt)
    {
        // evt.Exception may be null; provide a fallback message.
        string msg = evt.Exception?.Message ?? "(no exception message)";
        return $"Workflow error: {msg}";
    }








    /// <summary>
    ///     Formats workflow output and appends any buffered streamed chunks that were not emitted by terminal agent events.
    /// </summary>
    /// <param name="evt">The workflow output event.</param>
    /// <returns>Formatted workflow output text for UI display.</returns>
    private string FormatWorkflowOutputEvent(WorkflowOutputEvent evt)
    {
        WorkflowOutputEvent? finalOutput = null;
        if (evt.Is<List<ChatMessage>>())
        {
            finalOutput = evt;
        }


        if (finalOutput?.As<List<ChatMessage>>() is { } transcript)
        {
            _reporter.ReportDebug("\n\n=== Final Conversation Transcript ===\n");
            foreach (ChatMessage message in transcript)
            {
                _reporter.ReportDebug($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
            }
        }

        string residualBufferedOutput = FlushAllAccumulatedResponses();
        if (string.IsNullOrWhiteSpace(residualBufferedOutput))
        {
            return $"Workflow output: {evt.Data}";
        }

        return $"Workflow output: {evt.Data}{Environment.NewLine}{residualBufferedOutput}";
    }








    private string FormatWorkflowStartedEvent(WorkflowStartedEvent evt)
    {
        return $"Workflow started: {evt.Data}";
    }








    private string FormatWorkflowWarningEvent(WorkflowWarningEvent evt)
    {
        return $"Workflow warning: {evt.Data}";
    }








    private string? GetEventDetails(WorkflowEvent evt)
    {
        return evt switch
        {
            WorkflowStartedEvent startedEvent => FormatWorkflowStartedEvent(startedEvent),
            AgentResponseEvent responseEvent => FormatAgentResponseEvent(responseEvent),
            AgentResponseUpdateEvent => null, // buffered; flushed on AgentResponseEvent or ExecutorCompletedEvent
            SubworkflowErrorEvent subworkflowError => FormatSubWorkflowErrorEvent(subworkflowError),
            SubworkflowWarningEvent subworkflowWarningEvent => throw new NotImplementedException(),

            ExecutorCompletedEvent completedEvent => FormatExecutorCompletedEvent(completedEvent),
            WorkflowOutputEvent outputEvent => FormatWorkflowOutputEvent(outputEvent),
            WorkflowErrorEvent errorEvent => FormatWorkflowErrorEvent(errorEvent),
            ExecutorFailedEvent failedEvent => FormatExecutorFailedEvent(failedEvent),
            WorkflowWarningEvent warningEvent => FormatWorkflowWarningEvent(warningEvent),
            ExecutorInvokedEvent invokedEvent => FormatExecutorInvokedEvent(invokedEvent),
            SuperStepCompletedEvent superStepCompletedEvent => FormatSuperStepCompletedEvent(superStepCompletedEvent),
            SuperStepStartedEvent superStepStartedEvent => FormatSuperStepStartedEvent(superStepStartedEvent),
            RequestInfoEvent requestInfoEvent => FormatRequestInfoEvent(requestInfoEvent),
            MagenticPlanCreatedEvent magenticPlanCreatedEvent => FormatMagenticPlanCreatedEvent(magenticPlanCreatedEvent),
            SuperStepEvent superStepEvent => FormatSuperStepEvent(superStepEvent),
            ExecutorEvent executorEvent => FormatExecutorEvent(executorEvent),
            MagenticProgressLedgerUpdatedEvent magenticProgressLedgerUpdatedEvent => FormatProgressLedger(magenticProgressLedgerUpdatedEvent),
            MagenticReplannedEvent magenticReplannedEvent => FormatReplannedEvent(magenticReplannedEvent),
            MagenticOrchestratorEvent magenticOrchestratorEvent => FormatOrchestratorEvent(magenticOrchestratorEvent),
            _ => $"Unknown event type: {evt.GetType().Name}"
        };
    }








    private StringBuilder GetStringBuilder()
    {
        lock (_lock)
        {
            return _pool.Count > 0 ? _pool.Pop() : new StringBuilder();
        }
    }








    private void ReturnStringBuilder(StringBuilder sb)
    {
        sb.Clear();
        lock (_lock)
        {
            _pool.Push(sb);
        }
    }
}