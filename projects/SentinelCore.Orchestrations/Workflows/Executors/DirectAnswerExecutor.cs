// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DirectAnswerExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executes a direct-answer workflow step by invoking an AIAgent to produce a textual response, reporting progress and
///     returning the agent's text output.
///     Consider a safety valve for human operator escalation if the agent fails to produce a valid response.
///     Consider implementing a retry mechanism for transient failures in agent execution.
///     Consider a safety filter to validate the agent's output before yielding it to the workflow context.
/// </summary>
/// <remarks>
///     On failure, exceptions are reported to the reporter and a user-visible fallback message is yielded;
///     cancellation is re-thrown so cooperative cancellation propagates. The executor calls agent.RunAsync to obtain an
///     AgentResponse and yields the resulting text to the workflow via IWorkflowContext.YieldOutputAsync.
/// </remarks>
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class DirectAnswerExecutor : Executor
{
    private readonly AIAgent _agent;
    private readonly ISystemReporter _reporter;
    private readonly AgentSession _session;








    /// <summary>
    ///     Initializes a new instance of the <see cref="DirectAnswerExecutor" /> class.
    /// </summary>
    /// <param name="agent">Model-backed AIAgent used to generate the answer for the workflow step.</param>
    /// <param name="session">The persistent agent session for conversational consistency.</param>
    /// <param name="reporter">ISystemReporter used to emit informational and error reports during execution.</param>
    public DirectAnswerExecutor(AIAgent agent, AgentSession session, ISystemReporter reporter) : base("DirectAnswer", ExecutorOutputPolicy.CreateExplicitYieldsOnly())
    {

        // Instance of TheCore agent used to generate answers for the workflow step. Must be non-null.
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));

        // Persistent session for the agent, ensuring conversational context is maintained across invocations. Must be non-null.
        _session = session ?? throw new ArgumentNullException(nameof(session));
        // ISystemReporter used to emit informational and error reports during execution. Must be non-null.
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Main executor entry point called by the MAF dispatcher. Provides uniform cross-cutting
    ///     concerns: logging, null validation, cooperative cancellation propagation, and structured
    ///     error handling. Domain logic lives in <see cref="ProcessMessageAsync" />.
    /// </summary>
    /// <param name="input">The <see cref="ChatMessage" /> containing the prompt for the agent.</param>
    /// <param name="context">The <see cref="IWorkflowContext" /> for yielding output.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's answer, or a user-visible fallback message on failure.</returns>
    /// <exception cref="OperationCanceledException">Re-thrown when execution is cancelled so cancellation propagates.</exception>
    /// <remarks>
    ///     On a null agent response or an execution failure the executor yields a user-visible
    ///     fallback message and returns early; it never falls through to a null response dereference.
    /// </remarks>
    [MessageHandler]
    public async ValueTask<ChatMessage?> HandleChatMessageAsync(ChatMessage? input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        try
        {
            if (input is null)
            {
                _reporter.ReportWarning($"[{Name}] received null input.");
                return await YieldFallbackAsync(context, "I am unable to process your request due to missing information.", cancellationToken).ConfigureAwait(false);
            }

            ChatMessage result = await ProcessMessageAsync(input, context, cancellationToken).ConfigureAwait(false);

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
            _reporter.ReportError($"[CRITICAL WORKFLOW ERROR] Failed at {Name}", ex);
            _reporter.ReportError($"Exception Type: {ex.GetType().Name}", ex);
            _reporter.ReportError($"Stack Trace: {ex.StackTrace}", ex);
            return await YieldFallbackAsync(context, "An error occurred while processing your request.", cancellationToken).ConfigureAwait(false);
        }
    }








    /// <summary>
    ///     Core processing logic: answers the message passed to this executor. The message text is
    ///     the prompt; shared-state <see cref="WorkFlowStateKeys.PROMPT" /> is only a fallback for
    ///     messages that arrive without body text. The agent's answer is yielded to the workflow
    ///     so the UI receives it.
    /// </summary>
    /// <param name="input">The message passed to this executor; its text is the prompt to answer.</param>
    /// <param name="context">The workflow context for shared-state reads and output yielding.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's answer, or a user-visible fallback message.</returns>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage input, IWorkflowContext context, CancellationToken cancellationToken)
    {
        string prompt = input.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            ChatMessage? storedPrompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);
            prompt = storedPrompt?.Text ?? string.Empty;
        }

        _reporter.ReportInfo($"[{Name}] Prompt resolved. Length: {prompt.Length}");

        if (string.IsNullOrWhiteSpace(prompt))
        {
            _reporter.ReportWarning($"[{Name}] had no prompt on the message or in shared state.");
            return await YieldFallbackAsync(context, "I am unable to provide a response at this time.", cancellationToken).ConfigureAwait(false);
        }

        /*
         * Need to change prompts and logic for spawning of tech agents.
         *
         */
        List<ChatMessage> instructions =
        [
                new(ChatRole.System, AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S),
                new(ChatRole.System, "Using the mcp tools in your toolbelt answer the questions ask of you. Do not fabricate answers."),
                new(ChatRole.User, prompt)
        ];

        AgentResponse agResponse = await _agent.RunAsync(instructions, _session, null, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(agResponse.Text))
        {
            _reporter.ReportWarning($"[{Name}] agent returned an empty response.");
            return await YieldFallbackAsync(context, "I am unable to provide a response at this time.", cancellationToken).ConfigureAwait(false);
        }

        // --- Success path: yield the agent's answer to the workflow context and return it ---
        ChatMessage result = new(ChatRole.Assistant, agResponse.Text);
        await context.YieldOutputAsync(result, cancellationToken).ConfigureAwait(false);
        return result;
    }








    /// <summary>
    ///     Yields a user-visible fallback <see cref="ChatMessage" /> to the workflow and returns it,
    ///     so every failure path reports and returns the same way instead of throwing past the boundary.
    /// </summary>
    /// <param name="context">The workflow context for output yielding.</param>
    /// <param name="message">The user-visible fallback text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fallback message that was yielded.</returns>
    private async ValueTask<ChatMessage> YieldFallbackAsync(IWorkflowContext context, string message, CancellationToken cancellationToken)
    {
        ChatMessage fallback = new(ChatRole.Assistant, message);
        await context.YieldOutputAsync(fallback, cancellationToken).ConfigureAwait(false);
        return fallback;
    }
}