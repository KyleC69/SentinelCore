// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CriticalAlert.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;





namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the RedAlert branch: a critical event was detected. Advances the case (if any)
///     to <see cref="CaseStatus.Alerted" /> and yields a user-visible critical alert message.
///     The hypothesis is passed through to the next executor.
/// </summary>
/// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
/// <param name="reporter">The system reporter for logging.</param>
internal sealed class CriticalAlert(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : Executor<ChatMessage>("CriticalError")
{
    /// <summary>
    ///     Advances the case to <see cref="CaseStatus.Alerted" /> when a case id is present in
    ///     shared state, then yields a critical alert message to the user.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    public override async ValueTask HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        reporter.ReportInfo($"RedAlert received — advancing case to Alerted where applicable for message {message}");


        Guid? caseId = await context.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", cancellationToken).ConfigureAwait(false);
        if (caseId is { } id && id != Guid.Empty)
        {
            try
            {
                await caseFlowEngine.AdvanceCaseAsync(id, CaseStatus.Alerted, cancellationToken).ConfigureAwait(false);
                reporter.ReportInfo($"Case {id} advanced to Alerted.");
            }
            catch (Exception ex)
            {
                // The case lifecycle transition failed — report it, but the critical
                // alert must still be delivered.
                reporter.ReportError($"Failed to advance case {id} to Alerted.", ex);
            }
        }

        string prompt = string.IsNullOrWhiteSpace(message.Text) ? "the signal" : $"\"{message}\"";
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"🚨 **Red Alert** — {prompt} was classified as a critical event requiring immediate human attention. "), cancellationToken).ConfigureAwait(false);


    }
}
