// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NewCaseExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.CaseFlowEngine.CaseFlow;
using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.CaseFlowEngine.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Data.DBEntities;
using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     NON-Agent executor that starts a new case and publishes the CaseId to the context
///     for other executors and to UI/loggers. As a case moves through the pipeline, the
///     non-agent executors build onto the case currently being investigated.
///     Each step is focused, clean, and deliberate — clear separation enforced.
/// </summary>
[YieldsOutput(typeof(ExecutorEnvelope))]
public sealed partial class NewCaseExecutor : Executor
{
    private readonly ICaseFlowEngine _caseEng;
    private readonly ISystemReporter _reporter;

    private ChatMessages _chatMessages = new ChatMessages();






    /// <summary>
    ///     Initializes a new instance of the <see cref="NewCaseExecutor" /> class.
    /// </summary>
    /// <param name="caseEng">The case flow engine for case lifecycle operations.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public NewCaseExecutor(ICaseFlowEngine caseEng, ISystemReporter reporter) : base("CreateNewCase")
    {
        _caseEng = caseEng ?? throw new ArgumentNullException(nameof(caseEng));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








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
    public async ValueTask<ExecutorEnvelope> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var envelope = new ExecutorEnvelope
        {
            SignalText = message.Text
        };

        Throw.IfNull(message);

        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution.");

        // --- Save Envelope to state ---
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, envelope, "SharedState", cancellationToken).ConfigureAwait(false);
        _reporter.ReportDebug($"[{Name}] Saved envelope to state: {envelope}");

        try
        {
            CaseEntity? result = await ProcessCaseAsync(message, context, cancellationToken).ConfigureAwait(false);
            if (!ReferenceEquals(result, null))
            {
                envelope.Case = result;
            }

            await context.YieldOutputAsync(envelope, cancellationToken);

        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {

            var fallback = $"⚠️ An internal error occurred in {Name}: {ex.Message}";
            await context.YieldOutputAsync(fallback, cancellationToken).ConfigureAwait(false);
            _reporter.ReportInfo($"[{Name}] {fallback} due to error.");
            _chatMessages.AddAssistantMessage(fallback);
        }

        return envelope;
    }








    /// <summary>
    ///     Core processing logic: creates a new case and publishes the CaseId to context.
    /// </summary>
    private async ValueTask<CaseEntity?> ProcessCaseAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        try
        {
            _reporter.ReportDebug("New case starting now.");

            if (!ReferenceEquals(message?.Text, null))
            {
                var sig = new Signal(message.Text, "UIPrompt");


                // Create a new case using the prompt text as the initial signal. Set the source to "UIPrompt" to indicate it originated from the user interface instead of an automated system
                int caseId = await _caseEng.CreateCaseAsync(sig.ToEntity(), cancellationToken).ConfigureAwait(false);

                // Starts new case, saves caseid to context.
                // Log action and publish to UI.
                _reporter.ReportInfo($"Case created with ID: {caseId}.");
                CaseEntity? caseFile = await _caseEng.GetCaseByIdAsync(caseId, cancellationToken).ConfigureAwait(false);



                // Set caseid so it can be picked up by future steps.
                await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, caseFile?.Id, "SharedState", cancellationToken).ConfigureAwait(false);

                return caseFile;

            }
        }
        catch (Exception e)
        {

            _reporter.ReportError(e.Message, e);
        }

        return null!;
    }
}