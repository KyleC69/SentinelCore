// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorTemplate.cs
// Author: Kyle L. Crowder
// Build Num:  091418

// ==============================================================================
// EXECUTOR CONVENTION TEMPLATE
// ==============================================================================
// This file is a REFERENCE TEMPLATE — do NOT instantiate it directly.
// Copy the pattern into a new executor file and replace TIn/TOut with your
// actual message types, then implement ProcessMessageAsync with domain logic.
//
// MANDATORY CONVENTION (see pattern-lock PL-9):
//
//   1. Class extends Executor (non-generic, no type params).
//   2. [YieldsOutput(typeof(TOut))] on the class for compile-time type validation.
//   3. [MessageHandler] on the handler method (NOT override).
//   4. Handler method named Handle{TIn}Async (e.g., HandleChatMessageAsync).
//   5. partial keyword on the class (required by MAF source generator).
//   6. Conventional constructor (not primary constructor) — primary constructors
//      cause issues with the MAF source generator.
//   7. HandleAsync structure MUST contain:
//      a. Start/completion logging via ISystemReporter
//      b. Null input validation with fallback return
//      c. OperationCanceledException propagation (never swallow)
//      d. Generic catch with ISystemReporter.ReportError + user-visible message
//      e. Fallback return on error (never throw past the executor boundary)
//   8. Domain logic lives in a private ProcessMessageAsync method.
//   9. ISystemReporter is the ONLY output channel — never Console.WriteLine.
//  10. Use ConfigureAwait(false) on all awaits.
//  11. Name property set from the executor Id string for logging.
// ==============================================================================

using SentinelCore.Contracts.Abstractions;

namespace SentinelCore.Orchestrations.Workflows.Executors;

/// <summary>
///     A reusable, production-grade MAF executor template.
///
///     Key features:
///     - Strong typing: validates input at runtime to prevent silent routing failures.
///     - Robust error handling: catches and logs all exceptions, including cooperative cancellation.
///     - Deterministic logging: emits start, progress, and completion markers for workflow tracing.
///     - Final response pattern: guarantees a well-formed TOut even under failure conditions.
///     - Context-safe: uses IWorkflowContext for shared-state updates and output yielding.
///     - Compile-time validation: [YieldsOutput] ensures the workflow graph type-checks.
///
///     Replace TIn and TOut with your actual message types.
/// </summary>
[YieldsOutput(typeof(SignalHypothesis))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class ExecutorTemplate : Executor
{
    private readonly ISystemReporter _reporter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutorTemplate" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter" /> is <c>null</c>.</exception>
    public ExecutorTemplate(ISystemReporter reporter) : base("ExecutorTemplate")
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
    public async ValueTask<SignalHypothesis> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
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
            return CreateFallbackResult();
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
            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(SignalHypothesis)} due to error.");

            return CreateFallbackResult();
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
    private SignalHypothesis CreateFallbackResult() => new()
    {
        NextStep = NextStep.EscalateToHumanOperator,
        Reasoning = "Fallback: executor did not produce a result."
    };
}
