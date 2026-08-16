// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowBase.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Text;

using SentinelCore.Abstractions;




namespace SentinelCore.Workflows;





public class WorkflowBase
{
    protected ISystemReporter _reporter;
    private readonly Dictionary<string, StringBuilder> _responseAccumulators = new(StringComparer.Ordinal);








    protected WorkflowBase(ISystemReporter reporter)
    {
        _reporter = reporter;
    }








    /// <summary>
    ///     Accumulates a streaming update chunk for the given executor.
    ///     The chunk is buffered and will be reported as part of the
    ///     complete message when the final <see cref="AgentResponseEvent" />
    ///     or <see cref="ExecutorCompletedEvent" /> arrives.
    /// </summary>
    private void AccumulateUpdate(string executorId, string chunk)
    {
        if (!_responseAccumulators.TryGetValue(executorId, out StringBuilder? sb))
        {
            sb = new StringBuilder();
            _responseAccumulators[executorId] = sb;
        }

        sb.Append(chunk);
    }








    /// <summary>
    ///     Flushes and returns the accumulated streaming chunks for the
    ///     specified executor, then removes the accumulator entry.
    /// </summary>
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








    /// <summary>
    ///     Formats the final agent response, prepending any accumulated
    ///     streaming update chunks for the same executor.
    /// </summary>
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








    private string FormateSubWorkflowErrorEvent(SubworkflowErrorEvent subworkflowError)
    {
        // No meaningful error string is currently available; return an empty string to avoid null.
        return string.Empty;
    }








    private string? GetEventDetails(WorkflowEvent evt)
    {
        return evt switch
        {
                WorkflowStartedEvent startedEvent => FormatWorkflowStartedEvent(startedEvent),
                AgentResponseEvent responseEvent => FormatAgentResponseEvent(responseEvent),
                AgentResponseUpdateEvent => null, // buffered; flushed on AgentResponseEvent or ExecutorCompletedEvent
                SubworkflowErrorEvent subworkflowError => FormateSubWorkflowErrorEvent(subworkflowError),
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








    public string ProcessEvent(WorkflowEvent evt)
    {
        // Validate the event
        ArgumentNullException.ThrowIfNull(evt);

        if (evt is SubworkflowErrorEvent subError)
        {
            _reporter.ReportError(subError.Exception, $"Sub-workflow '{subError.SubworkflowId}' failed: {subError.Data}");
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
    ///     Clears all accumulated streaming response chunks.
    ///     Call this at the start of each workflow execution to ensure
    ///     state from a previous run is not carried over.
    /// </summary>
    public void ResetEventAccumulators()
    {
        _responseAccumulators.Clear();
    }
}