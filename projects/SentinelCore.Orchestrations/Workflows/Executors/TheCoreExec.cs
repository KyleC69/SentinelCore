// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreExec.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Contracts.Abstractions;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     The Agent Executor is especially constructed to use an extended context tied to the life-cycle of the application.
///     It is created manually on first use and the session persists through turns.
///     TODO: research alternative persistence strategies and checkpointing
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
internal sealed partial class TheCoreExec : Executor
{
    private readonly AIAgent _agent;
    private readonly ISystemReporter _reporter;
    private readonly AgentSession _session;








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreExec" /> class.
    /// </summary>
    /// <param name="agent">The Core AI agent.</param>
    /// <param name="session">The persistent agent session.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public TheCoreExec(AIAgent agent, AgentSession session, ISystemReporter reporter) : base("TheCoreExec")
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _session = session ?? throw new ArgumentNullException(nameof(session));
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
    private ChatMessage CreateFallbackResult() => new(ChatRole.Assistant, "The Core agent was unable to process the signal. Please try again.");








    /// <summary>
    ///     Handles the Core agent execution by invoking the agent with a signal hypothesis.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The signal hypothesis to process.</param>
    /// <param name="context">The workflow context providing shared state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The agent's response as a <see cref="ChatMessage" />.</returns>
    [MessageHandler]
    public async ValueTask<ChatMessage> HandleSignalHypothesisAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(SignalHypothesis)}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
            return CreateFallbackResult();
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                result = CreateFallbackResult();
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

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return CreateFallbackResult();
        }
    }








    /// <summary>
    ///     Core processing logic: invokes the Core agent with the hypothesis and returns the response.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // Log the received message
        _reporter.ReportInfo($"Handling SignalHypothesis: {message.Hypothesis}");

        // Create chatmessage with hypothesis
        ChatMessage msg = new(ChatRole.User, message.Hypothesis);

        // Delegate execution to the internal executor implementation
        AgentRunOptions aro = new();

        AgentResponse result = await _agent.RunAsync(msg, _session, aro, cancellationToken).ConfigureAwait(false);

        // Log the result
        _reporter.ReportInfo($"Execution completed with result: {result.Text}");

        ChatMessage outMsg = new(ChatRole.Assistant, result.Text);

        // The return value is auto-yielded as workflow output (non-void handler
        // return + WithOutputFrom registration), so no explicit yield here —
        // an explicit yield would duplicate the message in the chat.
        return outMsg;
    }
}