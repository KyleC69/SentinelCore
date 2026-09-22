using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;




[YieldsOutput(typeof(string))]
public partial class TerminateWorkflow : Executor
{
    private ISystemReporter _reporter;

    public TerminateWorkflow(ISystemReporter reporter) : base("TerminateWorkflow")
    {
        _reporter = reporter;
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
    public async ValueTask HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // Send a user-visible message back to the UI
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"Processing your request in {Name}..."), cancellationToken).ConfigureAwait(false);

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(SignalHypothesis)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(SignalHypothesis)}."), cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            // --- Core logic ---
            SignalHypothesis result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(SignalHypothesis)}.");
                result = CreateFallbackResult();
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(SignalHypothesis)}");

            return;
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
            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(SignalHypothesis)} due to error.");

            return;
        }


    }








    /// <summary>
        ///     Core processing logic for the executor.
        ///     Replace this method with your actual domain logic.
        /// </summary>
        private async ValueTask<SignalHypothesis> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
        {
            // Example placeholder logic:
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            // Always return a valid result instance.
            return CreateFallbackResult();
        }



        /// <summary>
        ///     Creates a fallback result when the executor encounters an error or receives null input.
        ///     Override this in your real executor to provide a domain-appropriate fallback value.
        /// </summary>
        private SignalHypothesis CreateFallbackResult() => new() { NextStep = NextStep.EscalateToHumanOperator, Reasoning = "Fallback: executor did not produce a result." };





}
