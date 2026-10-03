// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyReviewExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class SafetyReviewExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutorTemplate" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter" /> is <c>null</c>.</exception>
    public SafetyReviewExecutor(ISystemReporter reporter) : base("SafetyReviewExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Main executor entry point called by the MAF dispatcher.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="context">The workflow context for shared-state updates and output yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result of processing, or a fallback value on error.</returns>
    [MessageHandler]
    public async ValueTask<ChatMessage> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // Send a user-visible message back to the UI
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"Processing your request in {Name}..."), cancellationToken).ConfigureAwait(false);

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
            return new ChatMessage(ChatRole.Assistant, $"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            // --- Core logic ---
            ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                result = new ChatMessage(ChatRole.Assistant, $"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(ChatMessage)}");

            return result;
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

            // Yield a user-visible message
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            // --- Status: fallback output ---
            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return new ChatMessage(ChatRole.Assistant, $"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");
        }
    }








    /// <summary>
    ///     Core processing logic for the review path. This executor is intentionally a small,
    ///     conditional workflow branch used when a prompt is flagged for manual review.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        // Not Yet Implemented: This is a placeholder for the actual safety review logic.

        return message;
    }
}