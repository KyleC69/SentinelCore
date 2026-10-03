// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AggregationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executor that receives the evidence-gathering sub-workflow's synthesized results
///     (a <see cref="ChatMessage" /> from the MAG manager) and converts them into a
///     <see cref="NextStep" /> routed to the human-review branch, carrying the
///     evidence forward as the hypothesis reasoning.
/// </summary>
public sealed partial class AggregationExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregationExecutor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging.</param>
    public AggregationExecutor(ISystemReporter reporter) : base("Aggregator")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    private ChatMessage? CreateFallbackResult()
    {
        _reporter.ReportInfo($"[{Name}] Creating fallback result.");

        return new ChatMessage(ChatRole.Assistant, "⚠️ Unable to process the message. Please try again or escalate for further assistance.");
    }








    /// <summary>
    ///     Handles the aggregation of evidence-gathering results into a hypothesis routed for human review.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The synthesized evidence results from the MAG sub-workflow.</param>
    /// <param name="context">The workflow context providing shared state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A hypothesis routed to <see cref="NextStep.EscalateToHumanOperator" /> carrying the evidence.</returns>
    [MessageHandler]
    public async ValueTask HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken stoptoken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(NextStep)}.");
            ChatMessage msg = new(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(NextStep)}.");
            await context.YieldOutputAsync(msg).ConfigureAwait(false);
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");


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

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), stoptoken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

        }
    }








    /// <summary>
    ///     Converts the evidence-gathering results message into a hypothesis routed for
    ///     human review, preserving the original prompt and the evidence text.
    /// </summary>
    private async ValueTask ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo($"[{Name}] Aggregating evidence results: {message.Text}");

        // Recover the original prompt so the human-review message can reference it.
        string? prompt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);

    }
}