// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SubWorkflowExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.Agents.AI.Workflows.Specialized.Magentic;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





[YieldsOutput(typeof(ChatMessage))]
public sealed partial class SubWorkflowExecutor : Executor
{
    private readonly Workflow _evidence;
    private readonly ISystemReporter _reporter;








    public SubWorkflowExecutor(Workflow evidenceGather, ISystemReporter reporter) : base("SubWorkflowExecutor")
    {
        Name = Id;
        _evidence = evidenceGather ?? Throw.IfNull(evidenceGather);
        _reporter = reporter;
    }








    public string Name { get; }








    [MessageHandler]
    public async ValueTask<ChatMessage> HandleMessage(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // Implement the logic for handling the ChatMessage here
        // For example, you can log the message or perform some processing


        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(_evidence, message, cancellationToken: cancellationToken).ConfigureAwait(false);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(cancellationToken))
        {
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    // Stream per-participant deltas. Group by ResponseId / MessageId / ExecutorId so
                    // each new contiguous response prints its executor header once.
                    string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;
                    if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
                    {
                        if (lastResponseId is not null)
                        {
                            _reporter.ReportDebug("");
                        }

                        _reporter.ReportDebug($"- {updateEvent.ExecutorId}: ");
                        lastResponseId = responseId;
                    }

                    _reporter.ReportDebug(updateEvent.Update.Text);
                    break;

                case MagenticPlanCreatedEvent planCreated:
                    _reporter.ReportDebug($"\\n[Magentic Initial Plan]\\n{planCreated.FullTaskLedger.Text}");
                    break;

                case MagenticReplannedEvent replanned:
                    _reporter.ReportDebug($"\\n[Magentic Replanned]\\n{replanned.FullTaskLedger.Text}");
                    break;

                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                    _reporter.ReportDebug($"\\n[Magentic Progress Ledger] satisfied={ledger.IsRequestSatisfied}, " + $"inLoop={ledger.IsInLoop}, progressing={ledger.IsProgressBeingMade}, " + $"nextSpeaker={ledger.NextSpeaker}, instruction={ledger.InstructionOrQuestion}");
                    break;

                case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                    finalOutput = outputEvent;
                    break;

                case WorkflowErrorEvent workflowError:
                    _reporter.ReportDebug(workflowError.Exception?.ToString() ?? "Unknown workflow error.");
                    break;

                case ExecutorFailedEvent executorFailed:
                    _reporter.ReportDebug($"Executor '{executorFailed.ExecutorId}' failed: " + (executorFailed.Data?.ToString() ?? "unknown error"));
                    break;
            }
        }

        RunStatus status = await run.GetStatusAsync().ConfigureAwait(false);
        return message;
    }
}