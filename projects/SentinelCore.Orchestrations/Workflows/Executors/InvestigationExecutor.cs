// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Text;

using SentinelCore.Abstractions;
using SentinelCore.Events;




namespace SentinelCore.Workflows.Executors;





// ─────────────────────────────────────────────────────────────────────────────────
//  Executor Wrappers — bridge between WorkflowBuilder.AddSwitch and sub-workflows
// ─────────────────────────────────────────────────────────────────────────────────





/// <summary>
///     Executor that runs the Magentic investigation sub-workflow when the
///     <see cref="NextStep.Investigate" /> route is selected.
/// </summary>
public sealed partial class InvestigationExecutor() : Executor("InvestigationExecutor")
{
    // These dependencies are injected via constructor and will always be provided by DI.
    // Mark as nullable to satisfy the compiler when fields are not assigned inline.
    private readonly ISentinelCoreEvents? _events;
    private readonly Workflow? _investigationWorkflow;
    private readonly ISystemReporter? _reporter;








    public InvestigationExecutor(Workflow investigationWorkflow, ISentinelCoreEvents events, ISystemReporter reporter) : this()
    {
        _investigationWorkflow = investigationWorkflow;
        _events = events;
        _reporter = reporter;
    }








    [MessageHandler]
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(nameof(InvestigationExecutor), "InvestigationExecutor: Starting Magentic investigation…", ActivityType.System));

        // _investigationWorkflow is injected and guaranteed non-null by the container; use null-forgiving to satisfy the compiler.
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(_investigationWorkflow!, new ChatMessage(ChatRole.User, message), cancellationToken: cancellationToken).ConfigureAwait(false);

        await run.TrySendMessageAsync(new TurnToken(true)).ConfigureAwait(false);

        StringBuilder resultBuilder = new();

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            if (evt is AgentResponseUpdateEvent updateEvent)
            {
                _reporter?.ReportInfo($"[Investigation:{updateEvent.ExecutorId}]: {updateEvent.Update.Text}");
            }
            else if (evt is WorkflowOutputEvent outputEvent && outputEvent.Is<List<ChatMessage>>())
            {
                List<ChatMessage>? outputs = outputEvent.As<List<ChatMessage>>();
                if (outputs != null)
                {
                    foreach (ChatMessage msg in outputs)
                        if (!string.IsNullOrEmpty(msg?.Text))
                        {
                            resultBuilder.AppendLine(msg.Text);
                        }
                }
            }

        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(nameof(InvestigationExecutor), "InvestigationExecutor: Magentic investigation completed.", ActivityType.System));

        return resultBuilder.ToString();
    }








    [MessageHandler]
    private ValueTask<string> HandleStringAsync(string message, IWorkflowContext context)
    {

        // Example implementation: Log the received message and return it
        context.YieldOutputAsync("Pattern Match Found");
        return ValueTask.FromResult(message);
    }
}
