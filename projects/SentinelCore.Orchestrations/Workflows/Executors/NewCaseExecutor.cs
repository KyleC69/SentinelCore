// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NewCaseExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Orchestrations.Agents;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     NON-Agent executor that starts a new case and publishes the CaseId to the context
///     for other executors and to UI/loggers. As a case moves through the pipeline, the
///     non-agent executors build onto the case currently being investigated.
///     Each step is focused, clean, and deliberate — clear separation enforced.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class NewCaseExecutor : Executor
{
    private readonly ICaseFlowEngine _caseEng;
    private readonly ISystemReporter _reporter;








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
    public async ValueTask<ChatMessage> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(NextStep)}");

        // --- Null validation ---
        ChatMessage prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException();


        if (string.IsNullOrEmpty(prompt.Text))
        {
            _reporter.ReportWarning($"[{Name}] Prompt is null or empty. Cannot create a new case without a prompt.");
            ChatMessage fallback = new(ChatRole.Assistant, "⚠️ Cannot create a new case as the prompt is missing. Please provide a prompt.");
            await context.YieldOutputAsync(fallback, cancellationToken).ConfigureAwait(false);
            return message;
        }





        // --- Action: Create New Case ---
        // This executor's primary responsibility is to create a new case.
        // The prompt is used as the initial signal for the case.
        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            string? result = await ProcessMessageAsync(prompt, context, cancellationToken).ConfigureAwait(false);
            await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, result, "SharedState", cancellationToken);

            if (result is null)
            {
                /*  _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                  result = new ChatMessage(ChatRole.Assistant, $"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                  // Optionally yield here if you want immediate UI publish:
                  await context.YieldOutputAsync(result, cancellationToken).ConfigureAwait(false);
                */
            }



            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output Case Id: {result}");
            ChatMessage msgs = new ChatMessage().AddUserMessage(message.Text);

            return msgs;

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

            ChatMessage fallback = new(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}");
            await context.YieldOutputAsync(fallback, cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return fallback;
        }
    }








    /// <summary>
    ///     Core processing logic: creates a new case and publishes the CaseId to context.
    /// </summary>
    private async ValueTask<string> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportDebug("New case starting now.");



        ChatMessage? prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);

        if (!ReferenceEquals(prompt?.Text, null))
        {
            Guid caseId = await _caseEng.CreateCaseAsync(new Signal(prompt?.Text, "UIPrompt"), cancellationToken).ConfigureAwait(false);

            // Starts new case, saves caseid to context.
            // Log action and publish to UI.
            _reporter.ReportInfo($"New case created with ID: {caseId}.");

            // Set caseid so it can be picked up by future steps.
            await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, caseId, "SharedState", cancellationToken).ConfigureAwait(false);

            // The returned hypothesis is auto-yielded as workflow output (non-void
            // handler return + WithOutputFrom registration); an explicit yield here
            // would duplicate it.
            return caseId.ToString();
        }

        return "No reponse from case engine. Case not created.";
    }
}