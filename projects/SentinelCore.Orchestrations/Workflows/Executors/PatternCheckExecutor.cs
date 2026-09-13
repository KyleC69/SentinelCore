// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternCheckExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Performs a search in pattern memory for similar signals that may have been solved before
///     Will prepend relevant information that may help initial hypothesis
/// </summary>
public sealed class PatternCheckExecutor : Executor<ChatMessage, ChatMessage>
{
    private readonly ISystemReporter _reporter;








    public PatternCheckExecutor(ISystemReporter reporter) : base("PatternCheckExecutor")
    {
        _reporter = reporter;
    }








    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {
        _reporter.ReportInfo("Starting pattern check executor");


        _reporter.ReportInfo("Saving initial message to context");
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message.Text, "SharedState", token).ConfigureAwait(false);

        // Example implementation: Log the received message and return it
        await context.YieldOutputAsync(new ChatMessage(ChatRole.User, "Pattern Match Found"), token).ConfigureAwait(false);
        return message;
    }
}