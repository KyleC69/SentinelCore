// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MoreInformationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;





namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the MoreInformationRequired branch: advances the case (if any) to
///     <see cref="CaseStatus.AwaitingInput" /> and yields a user-visible request for
///     additional information. The hypothesis is passed through to the next executor.
/// </summary>
/// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
/// <param name="reporter">The system reporter for logging.</param>
public sealed class MoreInformationExecutor(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : Executor<SignalHypothesis, SignalHypothesis>("MoreInformationStep")
{
    /// <summary>
    ///     Advances the case to <see cref="CaseStatus.AwaitingInput" /> when a case id is
    ///     present in shared state, then yields a request for more information to the user.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    public override async ValueTask<SignalHypothesis> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        reporter.ReportInfo("More information required — advancing case to AwaitingInput where applicable.");

        Guid? caseId = await context.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", cancellationToken).ConfigureAwait(false);
        if (caseId is { } id && id != Guid.Empty)
        {
            try
            {
                await caseFlowEngine.AdvanceCaseAsync(id, CaseStatus.AwaitingInput, cancellationToken).ConfigureAwait(false);
                reporter.ReportInfo($"Case {id} advanced to AwaitingInput.");
            }
            catch (Exception ex)
            {
                // The case lifecycle transition failed — report it, but the user-facing
                // information request must still be delivered.
                reporter.ReportError($"Failed to advance case {id} to AwaitingInput.", ex);
            }
        }

        string prompt = string.IsNullOrWhiteSpace(message.OrigPrompt) ? "the signal" : $"\"{message.OrigPrompt}\"";
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"ℹ️ More information is required to investigate {prompt}. {message.Reasoning ?? string.Empty}"), cancellationToken).ConfigureAwait(false);

        return message;
    }
}