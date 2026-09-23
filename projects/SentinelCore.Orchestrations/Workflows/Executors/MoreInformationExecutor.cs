// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MoreInformationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Handles the MoreInformationRequired branch: advances the case (if any) to
///     <see cref="CaseStatus.AwaitingInput" /> and yields a user-visible request for
///     additional information. The hypothesis is passed through to the next executor.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
[YieldsOutput(typeof(SignalHypothesis))]
public sealed partial class MoreInformationExecutor : Executor
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="MoreInformationExecutor" /> class.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine used to advance the case lifecycle.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public MoreInformationExecutor(ICaseFlowEngine caseFlowEngine, ISystemReporter reporter) : base("MoreInformationStep")
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
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private SignalHypothesis CreateFallbackResult() => new() { NextStep = NextStep.MoreInformationRequired, Reasoning = "Fallback: more information executor did not produce a result." };








    /// <summary>
    ///     Handles the request for more information by advancing the case and yielding a user-visible notice.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The classified signal hypothesis.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis, passed through unchanged.</returns>
    [MessageHandler]
    public async ValueTask<SignalHypothesis> HandleSignalHypothesisAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(SignalHypothesis)}");

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

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(SignalHypothesis)} due to error.");

            return CreateFallbackResult();
        }
    }








    /// <summary>
    ///     Core processing logic: advances the case to AwaitingInput and yields a request for more information.
    /// </summary>
    private async ValueTask<SignalHypothesis> ProcessMessageAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo("More information required — advancing case to AwaitingInput where applicable.");

        Guid? caseId = await context.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", cancellationToken).ConfigureAwait(false);
        if (caseId is { } id && id != Guid.Empty)
        {
            try
            {
                await _caseFlowEngine.AdvanceCaseAsync(id, CaseStatus.AwaitingInput, cancellationToken).ConfigureAwait(false);
                _reporter.ReportInfo($"Case {id} advanced to AwaitingInput.");
            }
            catch (Exception ex)
            {
                // The case lifecycle transition failed — report it, but the user-facing
                // information request must still be delivered.
                _reporter.ReportError($"Failed to advance case {id} to AwaitingInput.", ex);
            }
        }

        string prompt = string.IsNullOrWhiteSpace(message.OrigPrompt) ? "the signal" : $"\"{message.OrigPrompt}\"";
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"ℹ️ More information is required to investigate {prompt}. {message.Reasoning ?? string.Empty}"), cancellationToken).ConfigureAwait(false);

        return message;
    }
}