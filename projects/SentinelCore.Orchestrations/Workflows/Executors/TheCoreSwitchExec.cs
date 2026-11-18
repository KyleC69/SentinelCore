using System.Text.Json;

using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;

using SentinelCore.Contracts.Abstractions;





[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class TheCoreSwitchExec : Executor
{
    private readonly ISystemReporter _reporter;
    private AIAgent _switchAgent;
    private readonly ISentinelAgentFactory _factory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutorTemplate" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter" /> is <c>null</c>.</exception>
    public TheCoreSwitchExec(ISystemReporter reporter, ISentinelAgentFactory factory) : base("TheCoreSwitchExec")
    {
        _factory = factory;
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; } = "TheCoreSwitchExec";








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

        _switchAgent = await _factory.CreateAgentAsync(AgentPreset.UtilityAgent, AgentToolSelection.None, SWITCH_INSTRUCTIONS, ChatResponseFormat.ForJsonSchema<CoreSwitchDecision>(), CancellationToken.None);
        //===================================================================


        // Send a user-visible message back to the UI
        await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"Processing your request in {Name}..."), cancellationToken).ConfigureAwait(false);

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            // --- Core logic ---
            if (!ReferenceEquals(message, null))
            {
                // ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

                await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message);

                // Invoke the agent for spam detection
                var response = await this._switchAgent.RunAsync<CoreSwitchDecision>(message, null, cancellationToken: cancellationToken).ConfigureAwait(false);
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

        return default;
    }








    private const string SWITCH_INSTRUCTIONS = """
                                               You are a highly skilled linguist and you are operating in Sentinel Core Forensic Investigation Platform.
                                               Your job is to classify the input signal you will receive and decide if it is general chat conversation does not require investigation.
                                               You will respond with true, it is general chat conversation - OR - false, it is not chat and requires investigation.
                                               Apply the following rules when making your decision:
                                               
                                               Investigation inputs:
                                               Log entries from any source.
                                               Telemetry like input.
                                               Any input starting with "Investigate" ie. Investigate poor SQL queries
                                               Input requesting any information from the 
                                               """;








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
    /// Creates a condition for routing messages based on the expected spam detection result.
    /// </summary>
    /// <param name="expectedResult">The expected spam detection result</param>
    /// <returns>A function that evaluates whether a message meets the expected result</returns>
    private static Func<object?, bool> GetCondition(bool expectedResult) =>
            detectionResult => detectionResult is CoreSwitchDecision result && result.IsDirect == expectedResult;













}
