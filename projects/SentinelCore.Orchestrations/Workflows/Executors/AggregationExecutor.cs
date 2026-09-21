// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AggregationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;





namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executor that receives the evidence-gathering sub-workflow's synthesized results
///     (a <see cref="ChatMessage" /> from the MAG manager) and converts them into a
///     <see cref="SignalHypothesis" /> routed to the human-review branch, carrying the
///     evidence forward as the hypothesis reasoning.
/// </summary>
/// <param name="reporter">The system reporter for logging.</param>
public sealed class AggregationExecutor(ISystemReporter reporter) : Executor<ChatMessage, SignalHypothesis>("Aggregator")
{
    /// <summary>
    ///     Converts the evidence-gathering results message into a hypothesis routed for
    ///     human review, preserving the original prompt and the evidence text.
    /// </summary>
    /// <param name="message">The synthesized evidence results from the MAG sub-workflow.</param>
    /// <param name="context">The workflow context providing shared state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A hypothesis routed to <see cref="NextStep.EscalateToHumanOperator" /> carrying the evidence.</returns>
    public override async ValueTask<SignalHypothesis> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        reporter.ReportInfo($"Aggregating evidence results: {message.Text}");

        // Recover the original prompt so the human-review message can reference it.
        string? prompt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);

        SignalHypothesis hypothesis = new()
        {
            NextStep = NextStep.EscalateToHumanOperator,
            OrigPrompt = prompt ?? message.Text,
            Hypothesis = "Evidence gathering completed.",
            Reasoning = message.Text
        };

        return hypothesis;
    }
}