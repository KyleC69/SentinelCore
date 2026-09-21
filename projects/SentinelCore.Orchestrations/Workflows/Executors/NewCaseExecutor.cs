// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NewCaseExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;

namespace SentinelCore.Orchestrations.Workflows.Executors;

/// <summary>
///     NON-Agent executor that starts a new case and publishes the CaseId to the context
///     for other executors and to UI/loggers. As a case moves through the pipeline, the
///     non-agent executors build onto the case currently being investigated.
///     Each step is focused, clean, and deliberate — clear separation enforced.
/// </summary>
[YieldsOutput(typeof(SignalHypothesis))]
public sealed partial class NewCaseExecutor : Executor
{
    private readonly ICaseFlowEngine _caseEng;
    private readonly ISystemReporter _reporter;

    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NewCaseExecutor" /> class.
    /// </summary>
    /// <param name="caseEng">The case flow engine for case lifecycle operations.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public NewCaseExecutor(ICaseFlowEngine caseEng, ISystemReporter reporter) : base("NewCase")
    {
        _caseEng = caseEng ?? throw new ArgumentNullException(nameof(caseEng));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }

    /// <summary>
    ///     Handles the creation of a new case from a signal hypothesis.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The signal hypothesis to create a case for.</param>
    /// <param name="context">The workflow context providing shared state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The original hypothesis, passed through after case creation.</returns>
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
    ///     Core processing logic: creates a new case and publishes the CaseId to context.
    /// </summary>
    private async ValueTask<SignalHypothesis> ProcessMessageAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.DebugMsg("New case starting now.");

        string? prmpt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);
        Guid caseId = await _caseEng.CreateCaseAsync(new Signal(message.Hypothesis ?? "No Hypothesis Entered", "User"), cancellationToken).ConfigureAwait(false);

        // Starts new case, saves caseid to context.
        // Log action and publish to UI.
        _reporter.ReportInfo($"New case created with ID: {caseId}.");

        // Set caseid so it can be picked up by future steps.
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, caseId, "SharedState", cancellationToken).ConfigureAwait(false);
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.SIGNAL_HYPOTHESIS, message, "SharedState", cancellationToken).ConfigureAwait(false);

        // The returned hypothesis is auto-yielded as workflow output (non-void
        // handler return + WithOutputFrom registration); an explicit yield here
        // would duplicate it.
        return message;
    }

    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private SignalHypothesis CreateFallbackResult() => new()
    {
        NextStep = NextStep.EscalateToHumanOperator,
        Reasoning = "Fallback: new case creation did not produce a result."
    };
}
