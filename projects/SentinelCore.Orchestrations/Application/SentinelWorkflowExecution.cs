// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelWorkflowExecution.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Events;




namespace SentinelCore.Application;





public interface ISentinelWorkflowExecution
{
    /// <summary>
    ///     Executes a <see cref="Workflow" /> with the given <see cref="ChatMessage" /> prompt,
    ///     capturing all streaming events and returning the final output.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="promptSignal">The user prompt to send into the workflow.</param>
    /// <param name="phaseLabel">
    ///     A short label used in log/event messages to identify which pipeline phase
    ///     is running (e.g. "Investigation", "Analysis", "Classification").
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A <see cref="WorkflowExecutionResult" /> containing the final output messages
    ///     (if any) and a summary of all captured events.
    /// </returns>
    Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] ChatMessage promptSignal, [NotNull] string phaseLabel, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Executes a <see cref="Workflow" /> with a raw text prompt, wrapping it
    ///     in a <see cref="ChatMessage" /> before delegating to the primary overload.
    /// </summary>
    Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] string promptText, [NotNull] string phaseLabel, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Executes a <see cref="Workflow" /> with the default phase label "Workflow".
    /// </summary>
    Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] ChatMessage promptSignal, CancellationToken cancellationToken = default);
}





/// <summary>
///     Universal workflow execution engine for the SentinelCore system.
///     <para>
///         Provides a single, consistent entry point for running any <see cref="Workflow" />
///         with full streaming event capture, structured logging via <see cref="ISystemReporter" />,
///         and lifecycle event publishing via <see cref="ISentinelCoreEvents" />.
///     </para>
///     <para>
///         All orchestration classes should delegate workflow execution to this class
///         rather than implementing their own <c>StreamingRun</c> / <c>WatchStreamAsync</c> loops.
///     </para>
/// </summary>
public sealed class SentinelWorkflowExecution : ISentinelWorkflowExecution
{
    private readonly ISentinelCoreEvents _eventing;
    private readonly SentinelCoreSettings _settings;
    private readonly ISystemReporter _systemReporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelWorkflowExecution" /> class.
    /// </summary>
    /// <param name="options">Application settings (required).</param>
    /// <param name="systemReporter">Error/info/warning reporter (required).</param>
    /// <param name="coreEvents">SentinelCore event hub (required).</param>
    /// <exception cref="ArgumentNullException">Any dependency is <c>null</c>.</exception>
    public SentinelWorkflowExecution([NotNull] IOptions<SentinelCoreSettings> options, [NotNull] ISystemReporter systemReporter, [NotNull] ISentinelCoreEvents coreEvents)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(systemReporter);
        ArgumentNullException.ThrowIfNull(coreEvents);

        _settings = options.Value;
        _systemReporter = systemReporter;
        _eventing = coreEvents;
    }








    // ─────────────────────────────────────────────────────────────────────────────
    //  Primary Execution — Workflow + ChatMessage
    // ─────────────────────────────────────────────────────────────────────────────








    /// <summary>
    ///     Executes a <see cref="Workflow" /> with the given <see cref="ChatMessage" /> prompt,
    ///     capturing all streaming events and returning the final output.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="promptSignal">The user prompt to send into the workflow.</param>
    /// <param name="phaseLabel">
    ///     A short label used in log/event messages to identify which pipeline phase
    ///     is running (e.g. "Investigation", "Analysis", "Classification").
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A <see cref="WorkflowExecutionResult" /> containing the final output messages
    ///     (if any) and a summary of all captured events.
    /// </returns>
    public async Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] ChatMessage promptSignal, [NotNull] string phaseLabel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(promptSignal);
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseLabel);
        _systemReporter.ReportInfo($"Starting to execute workflow named: {workflow.Name}");
        _eventing.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(phaseLabel, $"{phaseLabel}: Starting workflow execution…", ActivityType.System));
        Guid sessionid = Guid.NewGuid();
        List<WorkflowEventEntry> eventLog = new();
        List<ChatMessage>? finalMessages = null;

        try
        {
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, promptSignal, sessionid.ToString(), cancellationToken).ConfigureAwait(false);

            // REQUIRED: start the workflow
            //   await run.TrySendMessageAsync(new WorkflowInputEvent(promptSignal)).ConfigureAwait(false);

            // Now allow the workflow to run
            await run.TrySendMessageAsync(new TurnToken(true)).ConfigureAwait(false);

            await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                ProcessEvent(evt, phaseLabel, eventLog);

                if (evt is WorkflowOutputEvent outputEvent && outputEvent.Is<List<ChatMessage>>())
                {
                    finalMessages = outputEvent.As<List<ChatMessage>>();
                }
            }

            _eventing.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(phaseLabel, $"{phaseLabel}: Workflow execution completed.", ActivityType.System));
        }
        catch (Exception ex)
        {
            _systemReporter.ReportError(ex, $"{phaseLabel}: Workflow execution failed.");
            _eventing.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(phaseLabel, $"{phaseLabel}: Workflow execution failed — {ex.Message}", ActivityType.System));
            throw;
        }

        return new WorkflowExecutionResult(finalMessages, eventLog);
    }








    // ─────────────────────────────────────────────────────────────────────────────
    //  Overload — Workflow + string prompt
    // ─────────────────────────────────────────────────────────────────────────────








    /// <summary>
    ///     Executes a <see cref="Workflow" /> with a raw text prompt, wrapping it
    ///     in a <see cref="ChatMessage" /> before delegating to the primary overload.
    /// </summary>
    public Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] string promptText, [NotNull] string phaseLabel, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptText);

        return ExecuteAsync(workflow, new ChatMessage(ChatRole.User, promptText), phaseLabel, cancellationToken);
    }








    // ─────────────────────────────────────────────────────────────────────────────
    //  Overload — Workflow + ChatMessage, no phase label (uses "Workflow")
    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    ///     Executes a <see cref="Workflow" /> with the default phase label "Workflow".
    /// </summary>
    public Task<WorkflowExecutionResult> ExecuteAsync([NotNull] Workflow workflow, [NotNull] ChatMessage promptSignal, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(workflow, promptSignal, "Workflow", cancellationToken);
    }








    // ─────────────────────────────────────────────────────────────────────────────
    //  Event Processing
    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    ///     Processes a single <see cref="WorkflowEvent" />, routing it to the
    ///     appropriate <see cref="ISystemReporter" /> and <see cref="ISentinelCoreEvents" />
    ///     channels, and recording it in the event log.
    /// </summary>
    /// <param name="workflowEvent">The event to process.</param>
    /// <param name="phaseLabel">Label identifying the pipeline phase.</param>
    /// <param name="eventLog">Accumulator for structured event entries.</param>
    internal void ProcessEvent([NotNull] WorkflowEvent workflowEvent, [NotNull] string phaseLabel, [NotNull] List<WorkflowEventEntry> eventLog)
    {
        ArgumentNullException.ThrowIfNull(workflowEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseLabel);
        ArgumentNullException.ThrowIfNull(eventLog);

        switch (workflowEvent)
        {
            case AgentResponseUpdateEvent updateEvent:
                _systemReporter.ReportInfo($"[{phaseLabel}:{updateEvent.ExecutorId}]: {updateEvent.Update.Text}");
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.AgentResponse, updateEvent.ExecutorId, updateEvent.Update.Text));
                break;

            case MagenticPlanCreatedEvent planCreated:
                _eventing.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(phaseLabel, $"[{phaseLabel} Plan Created]\n{planCreated.FullTaskLedger.Text}", ActivityType.Manager));
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.MagenticPlanCreated, "MagenticManager", planCreated.FullTaskLedger.Text));
                break;

            case MagenticReplannedEvent replanned:
                _eventing.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(phaseLabel, $"[{phaseLabel} Replanned]\n{replanned.FullTaskLedger.Text}", ActivityType.Manager));
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.MagenticReplanned, "MagenticManager", replanned.FullTaskLedger.Text));
                break;

            case MagenticProgressLedgerUpdatedEvent progressUpdated:
                MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                string progressMessage = $"[{phaseLabel} Progress] satisfied={ledger.IsRequestSatisfied}, " + $"inLoop={ledger.IsInLoop}, progressing={ledger.IsProgressBeingMade}, " + $"nextSpeaker={ledger.NextSpeaker}";
                _systemReporter.ReportInfo(progressMessage);
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.MagenticProgress, "MagenticManager", progressMessage));
                break;

            case WorkflowOutputEvent outputEvent:
                _systemReporter.ReportInfo($"[{phaseLabel}] Workflow output received.");
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.WorkflowOutput, phaseLabel, "Output received"));
                break;

            case WorkflowErrorEvent errorEvent:
                _systemReporter.ReportError(errorEvent.Exception ?? new InvalidOperationException($"{phaseLabel} workflow error."), $"{phaseLabel} workflow failed.");
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.Error, phaseLabel, errorEvent.Exception != null ? errorEvent.Exception.Message : "Unknown error"));
                break;

            case ExecutorFailedEvent executorFailed:
                string failedMessage = $"Executor '{executorFailed.ExecutorId}' failed: {executorFailed.Data}";
                _systemReporter.ReportError(new InvalidOperationException(failedMessage), $"Executor '{executorFailed.ExecutorId}' failed in {phaseLabel}.");
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.ExecutorFailed, executorFailed.ExecutorId, failedMessage));
                break;

            default:
                // Capture unknown event types for forward-compatibility
                eventLog.Add(new WorkflowEventEntry(WorkflowEventType.Unknown, phaseLabel, workflowEvent.GetType().Name));
                break;
        }
    }
}





/// <summary>
///     A structured entry representing a single event captured during workflow execution.
/// </summary>
public sealed class WorkflowEventEntry
{
    internal WorkflowEventEntry(WorkflowEventType eventType, [NotNull] string source, [NotNull] string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        EventType = eventType;
        Source = source;
        Message = message;
    }








    /// <summary>
    ///     The category of the workflow event.
    /// </summary>
    public WorkflowEventType EventType { get; }

    /// <summary>
    ///     The text payload of the event.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     The executor or agent that produced the event.
    /// </summary>
    public string Source { get; }

    /// <summary>
    ///     When the event was captured (UTC).
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.Now;
}





/// <summary>
///     Categorizes the types of events that can occur during workflow execution.
/// </summary>
public enum WorkflowEventType
{
    /// <summary>An agent produced a response delta.</summary>
    AgentResponse,

    /// <summary>The Magentic manager created an initial plan.</summary>
    MagenticPlanCreated,

    /// <summary>The Magentic manager replanned.</summary>
    MagenticReplanned,

    /// <summary>The Magentic progress ledger was updated.</summary>
    MagenticProgress,

    /// <summary>The workflow produced its final output.</summary>
    WorkflowOutput,

    /// <summary>A workflow-level error occurred.</summary>
    Error,

    /// <summary>An executor failed.</summary>
    ExecutorFailed,

    /// <summary>An unrecognized event type was encountered.</summary>
    Unknown
}