// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AnalysisExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executor that runs the Analysis &amp; Grading group-concurrent sub-workflow.
/// </summary>
public partial class AnalysisExecutor(ISystemReporter reporter) : Executor<ChatMessage, ChatMessage>("AnalysisExecutor")
{
    private readonly ISentinelCoreEvents _events;
    private readonly ISystemReporter _reporter;







    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {



        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(nameof(AnalysisExecutor), "AnalysisExecutor: Starting Analysis & Grading sub-workflow…", ActivityType.System));
        /*
        //await using StreamingRun run = await InProcessExecution.RunStreamingAsync(_analysisWorkflow, new ChatMessage(ChatRole.User, message), cancellationToken: cancellationToken).ConfigureAwait(false);

        await run.TrySendMessageAsync(new TurnToken(true)).ConfigureAwait(false);

        StringBuilder resultBuilder = new();

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            if (evt is AgentResponseUpdateEvent updateEvent)
            {
                _reporter.ReportInfo($"[Analysis:{updateEvent.ExecutorId}]: {updateEvent.Update.Text}");
            }
            else if (evt is WorkflowOutputEvent outputEvent && outputEvent.Is<List<ChatMessage>>())
            {
                List<ChatMessage>? outputs = outputEvent.As<List<ChatMessage>>();
                if (outputs is not null)
                {
                    foreach (ChatMessage msg in outputs)
                        if (!string.IsNullOrEmpty(msg.Text))
                        {
                            resultBuilder.AppendLine(msg.Text);
                        }
                }
            }
        */

        return message; // resultBuilder.ToString();
    }




}