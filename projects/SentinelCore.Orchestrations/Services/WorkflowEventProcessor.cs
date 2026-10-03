// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowEventProcessor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



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








    public WorkflowEventProcessor(ISystemReporter reporter)
    {
        _reporter = reporter;
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

        // Buffer streaming update chunks; they are reported as part of the complete message
        if (evt is AgentResponseUpdateEvent updateEvent)
        {
            AccumulateUpdate(updateEvent.ExecutorId, updateEvent.Update.Text);
            return string.Empty; // Don't clutter UI with "buffered" messages
        }



        // Process event based on its type
        string? eventDetails = GetEventDetails(evt);

        if (eventDetails is null) return string.Empty;

        // Route error events to ReportError, others to ReportInfo
        if (evt is SubworkflowErrorEvent or ExecutorFailedEvent or WorkflowErrorEvent)
        {
            _reporter.ReportError(eventDetails, (evt as WorkflowErrorEvent)?.Exception ?? (evt as SubworkflowErrorEvent)?.Exception);
        }
        else if (evt is WorkflowOutputEvent)
        {
            //output to viewmodel to be added to chat Messages
            return eventDetails;
        }
        else
        {
            _reporter.ReportInfo(eventDetails);
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








    private void AccumulateUpdate(string executorId, string chunk)
    {
        lock (_lock)
        {
            if (!_responseAccumulators.TryGetValue(executorId, out StringBuilder? sb))
            {
                sb = GetStringBuilder();
                _responseAccumulators[executorId] = sb;
            }

            sb.Append(chunk);
        }
    }








    private string FlushAccumulatedResponse(string executorId)
    {
        StringBuilder? sb;
        lock (_lock)
        {
            if (!_responseAccumulators.Remove(executorId, out sb))
            {
                return string.Empty;
            }
        }

        string accumulated = sb.ToString();
        ReturnStringBuilder(sb);
        return accumulated;
    }








    private string FormatAgentResponseEvent(AgentResponseEvent evt)
    {
        string accumulated = FlushAccumulatedResponse(evt.ExecutorId);
        string text = evt.Response.Text;

        if (string.IsNullOrEmpty(accumulated))
        {
            return $"Agent response: {evt.ExecutorId}, Output: {text}";
        }

        // If text is already contained in accumulated, don't duplicate it.
        // Some frameworks put the whole thing in Response.Text at the end.
        if (accumulated.EndsWith(text, StringComparison.Ordinal) || text.StartsWith(accumulated, StringComparison.Ordinal))
        {
            return $"Agent response: {evt.ExecutorId}, Output: {(accumulated.Length >= text.Length ? accumulated : text)}";
        }

        return $"Agent response: {evt.ExecutorId}, Accumulated: {accumulated}, Output: {text}";
    }








    private string FormatExecutorCompletedEvent(ExecutorCompletedEvent evt)
    {
        string accumulated = FlushAccumulatedResponse(evt.ExecutorId);
        if (!string.IsNullOrEmpty(accumulated))
        {
            return $"Executor completed: {evt.ExecutorId}, Accumulated Output: {accumulated}";
        }

        return $"Executor completed: {evt.ExecutorId}";
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








    private string FormatWorkflowOutputEvent(WorkflowOutputEvent evt)
    {
        return $"Workflow output: {evt.ExecutorId} {evt.Data}";

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