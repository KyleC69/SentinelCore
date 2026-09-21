// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternCheckExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Performs a search in pattern memory for similar signals that may have been solved before
///     Will prepend relevant information that may help initial hypothesis
/// </summary>
public sealed class PatternCheckExecutor(ISystemReporter reporter) : Executor<ChatMessage, ChatMessage>("PatternCheckExecutor")
{














    //Intentionally pass the message through to the next executor, as this is a check and not a transformation
    // Not fully implemented, but this is a placeholder for future pattern matching logic

    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {
        reporter.ReportInfo("Starting pattern check executor");


        reporter.ReportInfo("Saving initial message to context");
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message.Text, "SharedState", token).ConfigureAwait(false);

        // Intentionally pass the message through to the next executor — this is a
        // check, not a transformation. No output is yielded: pattern matching is not
        // implemented yet and a placeholder yield would leak fake data into the chat.
        return message;
    }
}
