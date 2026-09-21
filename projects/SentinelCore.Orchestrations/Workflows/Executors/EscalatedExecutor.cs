// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         EscalatedExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;





namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the EscalateToHumanOperator branch: advances the case (if any) to
///     <see cref="CaseStatus.Escalated" /> and yields a user-visible escalation notice.
///     The hypothesis is passed through to the next executor.
/// </summary>
/// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
/// <param name="reporter">The system reporter for logging.</param>
internal sealed class EscalatedExecutor(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : Executor<SignalHypothesis, SignalHypothesis>("EscalatedExecutor")
{
    /// <summary>
    ///     Advances the case to <see cref="CaseStatus.Escalated" /> when a case id is present
    ///     in shared state, then yields an escalation notice to the user.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    public override async ValueTask<SignalHypothesis> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        reporter.ReportInfo("Escalating signal to human operator — advancing case to Escalated where applicable.");

        Guid? caseId = await context.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", cancellationToken).ConfigureAwait(false);
        if (caseId is { } id && id != Guid.Empty)
        {
            try
            {
                await caseFlowEngine.AdvanceCaseAsync(id, CaseStatus.Escalated, cancellationToken).ConfigureAwait(false);
                reporter.ReportInfo($"Case {id} advanced to Escalated.");
            }
            catch (Exception ex)
            {
                // The case lifecycle transition failed — report it, but the escalation
                // notice must still be delivered.
                reporter.ReportError($"Failed to advance case {id} to Escalated.", ex);
            }
        }

        string prompt = string.IsNullOrWhiteSpace(message.OrigPrompt) ? "the signal" : $"\"{message.OrigPrompt}\"";
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"👤 {prompt} has been escalated for human review. {message.Reasoning ?? string.Empty}"), cancellationToken).ConfigureAwait(false);

        return message;
    }
}
