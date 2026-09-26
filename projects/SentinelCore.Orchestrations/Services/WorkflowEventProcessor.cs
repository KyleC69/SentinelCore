// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations.Services
// File:         WorkflowEventProcessor.cs
// Author: AI Agent
// Build Num:  20260925

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Events;
using System.Text;

namespace SentinelCore.Orchestrations.Services;

/// <summary>
/// Concrete implementation of IWorkflowEventProcessor.
/// Handles the formatting and reporting of all workflow events,
/// including streaming accumulation and flushing.
/// </summary>
public class WorkflowEventProcessor : IWorkflowEventProcessor
{
    private readonly ISystemReporter _reporter;
    private readonly Dictionary<string, StringBuilder> _responseAccumulators = new(StringComparer.Ordinal);

    public WorkflowEventProcessor(ISystemReporter reporter)
    {
        _reporter = reporter;
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

        // Subworkflow error handling
        if (evt is SubworkflowErrorEvent subError)
        {
            _reporter.ReportError($"Sub-workflow '{subError.SubworkflowId}' failed: {subError.Data}", subError.Exception);
        }

        // Buffer streaming update chunks; they are reported as part of the complete message
        if (evt is AgentResponseUpdateEvent updateEvent)
        {
            AccumulateUpdate(updateEvent.ExecutorId, updateEvent.Update.Text);
            return $"Agent response update buffered: {updateEvent.ExecutorId}";
        }

        // Flush any accumulated chunks when the executor completes
        if (evt is ExecutorCompletedEvent completedEvent)
        {
            string accumulated = FlushAccumulatedResponse(completedEvent.ExecutorId);
            if (!string.IsNullOrEmpty(accumulated))
            {
                _reporter.ReportInfo($"Agent response (accumulated): {completedEvent.ExecutorId}, Output: {accumulated}");
            }
        }

        // Process event based on its type
        string? eventDetails = GetEventDetails(evt);

        // Publish event details using the system reporter (skip nulls from buffered events)
        if (eventDetails is not null)
        {
            _reporter.ReportInfo(eventDetails);
        }

        // Return the processed event details
        return eventDetails ?? string.Empty;
    }

    /// <summary>
    /// Clears all accumulated streaming response chunks.
    /// </summary>
    public void ResetEventAccumulators()
    {
        _responseAccumulators.Clear();
    }

    // --- Private Helper Methods (Copied from WorkflowBase) ---

    private void AccumulateUpdate(string executorId, string chunk)
    {
        if (!_responseAccumulators.TryGetValue(executorId, out StringBuilder? sb))
        {
            sb = new StringBuilder();
            _responseAccumulators[executorId] = sb;
        }

        sb.Append(chunk);
    }

    private string FlushAccumulatedResponse(string executorId)
    {
        if (!_responseAccumulators.Remove(executorId, out StringBuilder? sb))
        {
            return string.Empty;
        }

        string accumulated = sb.ToString();
        sb.Clear();
        return accumulated;
    }

    private string FormatAgentResponseEvent(AgentResponseEvent evt)
    {
        string accumulated = FlushAccumulatedResponse(evt.ExecutorId);
        return string.IsNullOrEmpty(accumulated) ? $"Agent response: {evt.ExecutorId}, Output: {evt.Response.Text}" : $"Agent response: {evt.ExecutorId}, Accumulated: {accumulated}, Output: {evt.Response.Text}";
    }

    private string FormatExecutorCompletedEvent(ExecutorCompletedEvent evt)
    {
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