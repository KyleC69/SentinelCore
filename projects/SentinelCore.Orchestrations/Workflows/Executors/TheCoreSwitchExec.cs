// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreSwitchExec.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using System.Text.Json;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Models;
using SentinelCore.Security;
using SentinelCore.Security.Quarantine;




namespace SentinelCore.Orchestrations.Workflows.Executors;





[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class TheCoreSwitchExec : Executor
{
    private readonly ISentinelAgentFactory _factory;
    private readonly ISentinelProvenanceService _provenanceService;
    private readonly IQuarantineStore _quarantineStore;
    private readonly ISystemReporter _reporter;
    private AIAgent _switchAgent;



    private const string SWITCH_INSTRUCTIONS = """
                                               You are a highly skilled linguist and you are operating in Sentinel Core Forensic Investigation Platform.
                                               Your job is to classify the input signal you will receive and decide if it is general chat conversation does not require investigation.
                                               You will respond with true, it is general chat conversation - OR - false, it is not chat and requires investigation.
                                               Apply the following rules when making your decision:

                                               Investigation inputs:
                                               Log entries from any source.
                                               Input resembling a system alert or error message.
                                               Any input from a user that suggests an investigation or analysis is needed.
                                               Input requesting any information from the 
                                               """;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutorTemplate" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <param name="factory">The agent factory used to create the switch agent.</param>
    /// <param name="provenanceService">The provenance service for model-boundary integrity re-validation.</param>
    /// <param name="quarantineStore">The store receiving messages that fail re-validation, unchanged.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter" /> is <c>null</c>.</exception>
    public TheCoreSwitchExec(ISystemReporter reporter, ISentinelAgentFactory factory, ISentinelProvenanceService provenanceService, IQuarantineStore quarantineStore) : base("TheCoreSwitchExec")
    {
        _factory = factory;
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _quarantineStore = quarantineStore ?? throw new ArgumentNullException(nameof(quarantineStore));
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; } = "TheCoreSwitchExec";








    /// <summary>
    ///     Creates a condition for routing messages based on the expected spam detection result.
    /// </summary>
    /// <param name="expectedResult">The expected spam detection result</param>
    /// <returns>A function that evaluates whether a message meets the expected result</returns>
    private static Func<object?, bool> GetCondition(bool expectedResult) =>
            detectionResult => detectionResult is CoreSwitchDecision result && result.IsDirect == expectedResult;








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
    public async ValueTask<CoreSwitchDecision?> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
        }


        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(ChatMessage)}");

        _switchAgent = await _factory.CreateAgentAsync(AgentPreset.UtilityAgent, AgentToolSelection.None, SWITCH_INSTRUCTIONS, ChatResponseFormat.ForJsonSchema<CoreSwitchDecision>(), cancellationToken);
        //===================================================================


        // Send a user-visible message back to the UI
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"Processing your request in {Name}..."), cancellationToken).ConfigureAwait(false);

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            // --- Defense in depth: re-validate provenance before any model sees the message ---
            if (!ReferenceEquals(message, null) && !ValidateIntegrity(message))
            {
                await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "⚠️ Message rejected: provenance validation failed. The message has been quarantined."), cancellationToken).ConfigureAwait(false);
                return null;
            }

            // --- Core logic ---
            if (!ReferenceEquals(message, null))
            {
                // ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

                await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message);


                var response = await _switchAgent.RunAsync<CoreSwitchDecision>(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                CoreSwitchDecision? detectionResult = JsonSerializer.Deserialize<CoreSwitchDecision>(response.Text);


                await context.SendMessageAsync(message).AsTask().ConfigureAwait(false);


                // message.WithAgentRequestMessageSource(MessageSourceType.)

                // --- Status: Final output ---
                _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(ChatMessage)}");

                return detectionResult;
            }

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
            return null;
        }

        return new CoreSwitchDecision(false, false);
    }








    /// <summary>
    ///     Core processing logic for the executor.
    ///     Replace this method with your actual domain logic.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // Example placeholder logic:
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        // Always return a valid result instance.
        return message;
    }








    /// <summary>
    ///     Defense-in-depth integrity re-check performed immediately before the switch
    ///     agent (the first model contact) runs. Quarantines the message unchanged when
    ///     validation does not pass; validation throwing is treated as a failure.
    /// </summary>
    /// <param name="message">The message to re-validate.</param>
    /// <returns><c>true</c> when the message may proceed to the model; <c>false</c> when it was quarantined.</returns>
    private bool ValidateIntegrity(ChatMessage message)
    {
        ProvenanceValidationResult integrity;

        try
        {
            integrity = _provenanceService.VerifyIntegrity(message);
        }
        catch (Exception ex)
        {
            _quarantineStore.Quarantine(message, ProvenanceFailure.InternalError, $"Integrity validation threw: {ex.Message}");
            _reporter.ReportError($"[{Name}] Integrity validation threw; message quarantined. {ex.Message}", ex);
            return false;
        }

        if (integrity.IsValid)
        {
            return true;
        }

        _quarantineStore.Quarantine(message, integrity.Failure, integrity.Detail);
        _reporter.ReportError($"[{Name}] Message quarantined at model boundary. Reason: {integrity.Failure}. Detail: {integrity.Detail}");
        return false;
    }
}