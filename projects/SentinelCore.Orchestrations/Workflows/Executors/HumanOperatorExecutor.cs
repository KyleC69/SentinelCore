// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         HumanOperatorExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;





namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Terminal executor for every path that requires human intervention: RedAlert,
///     MoreInformationRequired, EscalateToHumanOperator, and the switch default.
///     Acknowledges the escalation to the user via a yielded <see cref="ChatMessage" /> and
///     passes the hypothesis through so downstream consumers can inspect it.
/// </summary>
/// <param name="reporter">The system reporter for logging escalation activity.</param>
public sealed class HumanOperatorExecutor(ISystemReporter reporter) : Executor<SignalHypothesis, SignalHypothesis>("HumanOperator")
{
    /// <summary>
    ///     Handles the escalation of a signal to a human operator by reporting the escalation
    ///     and yielding a user-visible acknowledgement message.
    /// </summary>
    /// <param name="input">The signal hypothesis containing information about the signal.</param>
    /// <param name="context">The workflow context providing state and execution information.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation. The hypothesis is passed through unchanged.</returns>
    public override async ValueTask<SignalHypothesis> HandleAsync(SignalHypothesis input, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        reporter.ReportInfo($"Signal escalated to human operator. NextStep: {input.NextStep}, Reasoning: {input.Reasoning ?? "(none)"}");

        string prompt = string.IsNullOrWhiteSpace(input.OrigPrompt) ? "the signal" : $"\"{input.OrigPrompt}\"";
        string message = input.NextStep switch
        {
            NextStep.RedAlert => $"🚨 **Red Alert** — {prompt} was classified as a critical event and requires immediate human attention.",
            NextStep.MoreInformationRequired => $"ℹ️ More information is required to investigate {prompt}. Please provide additional details so the investigation can proceed.",
            _ => $"👤 {prompt} has been escalated to a human operator for review."
        };

        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, message), cancellationToken).ConfigureAwait(false);

        return input;
    }
}