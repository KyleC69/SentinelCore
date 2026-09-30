// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations.Services
// File:         WorkflowEventProcessor.cs
// Author: AI Agent
// Build Num:  20260925

using System.Text;

using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Services;

/// <summary>
/// Concrete implementation of IWorkflowEventProcessor.
/// Handles the formatting and reporting of all workflow events,
/// including streaming accumulation and flushing.
/// </summary>
public class WorkflowEventProcessor : IWorkflowEventProcessor
{
    private readonly ISystemReporter _reporter;
    private readonly object _lock = new();
    private readonly Dictionary<string, StringBuilder> _responseAccumulators = new(StringComparer.Ordinal);
    private readonly Stack<StringBuilder> _pool = new();

    public WorkflowEventProcessor(ISystemReporter reporter)
    {
        _reporter = reporter;
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

    /// <summary>
    /// Processes a single workflow event, handling formatting and reporting.
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
    /// Clears all accumulated streaming response chunks.
    /// </summary>
    public void ResetEventAccumulators()
    {
        lock (_lock)
        {
            foreach (var sb in _responseAccumulators.Values)
            {
                ReturnStringBuilder(sb);
            }
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

            WorkflowOutputEvent outputEvent => FormatWorkflowOutputEvent(outputEvent),
            WorkflowErrorEvent errorEvent => FormatWorkflowErrorEvent(errorEvent),
            WorkflowWarningEvent warningEvent => FormatWorkflowWarningEvent(warningEvent),
            ExecutorInvokedEvent invokedEvent => FormatExecutorInvokedEvent(invokedEvent),
            ExecutorCompletedEvent completedEvent => FormatExecutorCompletedEvent(completedEvent),
            ExecutorFailedEvent failedEvent => FormatExecutorFailedEvent(failedEvent),
            SuperStepStartedEvent superStepStartedEvent => FormatSuperStepStartedEvent(superStepStartedEvent),
            SuperStepCompletedEvent superStepCompletedEvent => FormatSuperStepCompletedEvent(superStepCompletedEvent),
            RequestInfoEvent requestInfoEvent => FormatRequestInfoEvent(requestInfoEvent),
            _ => $"Unknown event type: {evt.GetType().Name}"
        };
    }
}