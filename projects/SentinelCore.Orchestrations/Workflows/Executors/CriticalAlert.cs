// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CriticalAlert.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the RedAlert branch: a critical event was detected. Advances the case (if any)
///     to <see cref="CaseStatus.Alerted" /> and yields a user-visible critical alert message.
///     The hypothesis is passed through to the next executor.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
internal sealed partial class CriticalAlert : Executor
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="CriticalAlert" /> class.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public CriticalAlert(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : base("CriticalError")
    {
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Handles the RedAlert branch by advancing the case and yielding a critical alert.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    [MessageHandler]
    public async ValueTask HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Skipping execution.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Execution skipped."), cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully.");
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {
            // --- Robust error handling ---
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);
        }
    }








    /// <summary>
    ///     Core processing logic: advances the case to Alerted and yields a critical alert message.
    /// </summary>
    private async ValueTask ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo($"RedAlert received — advancing case to Alerted where applicable for message {message}");

        Guid? caseId = await context.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", cancellationToken).ConfigureAwait(false);
        if (caseId is { } id && id != Guid.Empty)
        {
            try
            {
                await _caseFlowEngine.AdvanceCaseAsync(id, CaseStatus.Alerted, cancellationToken).ConfigureAwait(false);
                _reporter.ReportInfo($"Case {id} advanced to Alerted.");
            }
            catch (Exception ex)
            {
                // The case lifecycle transition failed — report it, but the critical
                // alert must still be delivered.
                _reporter.ReportError($"Failed to advance case {id} to Alerted.", ex);
            }
        }

        string prompt = string.IsNullOrWhiteSpace(message.Text) ? "the signal" : $"\"{message}\"";
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"🚨 **Red Alert** — {prompt} was classified as a critical event requiring immediate human attention. "), cancellationToken).ConfigureAwait(false);
    }
}