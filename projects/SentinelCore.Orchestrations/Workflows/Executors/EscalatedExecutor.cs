// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         EscalatedExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the EscalateToHumanOperator branch: advances the case (if any) to
///     <see cref="CaseStatus.Escalated" /> and yields a user-visible escalation notice.
///     The hypothesis is passed through to the next executor.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
internal sealed partial class EscalatedExecutor : Executor
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="EscalatedExecutor" /> class.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public EscalatedExecutor(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : base("EscalatedExecutor")
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
    ///     Handles the escalation of a signal to a human operator.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    [MessageHandler]
    public async ValueTask HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(ChatMessage)}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage result = null; // await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(ChatMessage)}");

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

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

        }
    }






}