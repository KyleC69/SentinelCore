// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         HumanOperatorExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Terminal executor for every path that requires human intervention: RedAlert,
///     MoreInformationRequired, EscalateToHumanOperator, and the switch default.
///     Acknowledges the escalation to the user via a yielded <see cref="ChatMessage" /> and
///     passes the hypothesis through so downstream consumers can inspect it.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class HumanOperatorExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="HumanOperatorExecutor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging escalation activity.</param>
    public HumanOperatorExecutor(ISystemReporter reporter) : base("HumanOperator")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    /// <summary>
    ///     Handles the escalation of a signal to a human operator.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="input">The signal hypothesis containing information about the signal.</param>
    /// <param name="context">The workflow context providing state and execution information.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    [MessageHandler]
    public async ValueTask HandleChatMessageAsync(ChatMessage input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(ChatMessage)}");

        // --- Null validation ---
        if (input is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage result = new(); //await ProcessMessageAsync(input, context, cancellationToken).ConfigureAwait(false);

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








    /// <summary>
    ///     Core processing logic: acknowledges the escalation and yields a user-visible message.
    /// </summary>
    private async ValueTask ProcessMessageAsync(ChatMessage input, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "message"), cancellationToken).ConfigureAwait(false);

    }
}