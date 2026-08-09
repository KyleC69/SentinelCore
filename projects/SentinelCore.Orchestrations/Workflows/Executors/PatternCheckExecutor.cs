// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternCheckExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Abstractions;




namespace SentinelCore.Workflows.Executors;





/// <summary>
///     Performs a search in pattern memory for similar signals that may have been solved before
///     Will prepend relevant information that may help initial hypothesis
/// </summary>
public sealed class PatternCheckExecutor(ISystemReporter reporter) : Executor<ChatMessage, ChatMessage>("patterncheck")
{

    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {
        reporter.ReportInfo("Starting pattern check executor");


        reporter.ReportInfo("Saving initial message to context");
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message.Text, "Shared", token).ConfigureAwait(false);

        // Example implementation: Log the received message and return it
        await context.YieldOutputAsync(new ChatMessage(ChatRole.User, "Pattern Match Found"), token).ConfigureAwait(false);
        return message;
    }
}