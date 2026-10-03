// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreExec.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     The Agent Executor is especially constructed to use an extended context tied to the life-cycle of the application.
///     It is created manually on first use and the session persists through turns.
///     TODO: research alternative persistence strategies and checkpointing
/// </summary>
[YieldsOutput(typeof(InvestigationObjective))]
internal sealed partial class TheCoreExec : Executor
{
    private readonly AIAgent _agent;
    private readonly ISystemReporter _reporter;
    private readonly AgentSession _session;



    private string taskinstruct = """
                                  For this task you are to analyze the signal and provide a clear and concise hypothesis about what the signal is trying to indicate. 
                                  The signal may also be presented to you in natural language and describe a symptom or observed behavior. for example: "file history is not backing up files"
                                  or "the system boots slow and hangs at startup". You must form a hypothesis as to what might be causing the observed behavior. Your response should be structured and in the shape of
                                  the schema you are provided. You should also provide a confidence score between 0 and 1 indicating how confident you are in your hypothesis.

                                  Example output:

                                  Given a signal like: database service crashed as 12:32am

                                  Hypothesis:
                                  Database connection pool exhaustion caused service failure.

                                  Confidence:
                                  0.68

                                  Predictions:
                                  - Spike in active SQL connections
                                  - Increased connection timeout errors
                                  - Elevated request latency before failure
                                  - Application recovered after pool recycle

                                  Suggested Evidence Domains:
                                  - Application Logs
                                  - SQL Metrics
                                  - Performance Counters
                                  - Event Logs

                                  Contradictory Indicators:
                                  - Host resource exhaustion
                                  - Process termination by OS
                                  - Network failure between service and database

                                  """;








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
    ///     Core processing logic: invokes the Core agent with the signal and TheCore generates the hypothesis and returns the
    ///     structured response.
    /// </summary>
    private async ValueTask<InvestigationObjective> GenerateDecisionAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // This will be the original message received, which is a InvestigationObjective. We need to convert it to an InvestigationObjective for the agent. 
        //Phasing out the signal hypo object in favor of new shape
        // Log the received message
        _reporter.ReportInfo($"Handling InvestigationObjective: {message}");

        ChatMessage? prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false);
        //compare prompt to message and if they are different, log a warning
        if (prompt != null && !prompt.Text.Equals(message.Text, StringComparison.OrdinalIgnoreCase))
        {
            _reporter.ReportWarning($"Prompt mismatch: Retrieved prompt from state ({prompt}) does not match current message content ({message.Text}).");
        }

        // If the prompt from the context is null or empty, use the current message content as the prompt.
        // This ensures we always have a prompt to send to the agent.
        ChatMessage effectivePrompt = prompt ?? message;

        // Log the effective prompt being used
        _reporter.ReportInfo($"Using prompt for agent: {effectivePrompt.Text}");

        // Construct the full instruction set for the agent.
        // We include the platform domain, the general Sentinel Core instructions, and the task-specific instructions.
        // If there's a specific prompt from the workflow state (or the current message if none in state), it's added as a user message.
        ChatMessages cms = InstructionLayerBuilder.Build(AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S, AgentInstructionConstants.SENTINEL_CORE_INSTRUCTIONS, taskinstruct);
        cms.Add(prompt);

        // Delegate execution to the internal executor implementation
        AgentRunOptions aro = new();

        AgentResponse<InvestigationObjective> result = await _agent.RunAsync<InvestigationObjective>(cms, _session, null, aro, cancellationToken).ConfigureAwait(false);

        // Log the result
        _reporter.ReportInfo($"Execution completed with result: {result.Text}");

        //ChatMessage outMsg = new(ChatRole.Assistant, result.Text);

        // The return value is auto-yielded as workflow output (non-void handler
        // return + WithOutputFrom registration), so no explicit yield here —
        // an explicit yield would duplicate the message in the chat.
        return result.Result;
    }








    /// <summary>
    ///     Handles the Core agent execution by invoking the agent with a signal hypothesis.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The signal hypothesis to process.</param>
    /// <param name="context">The workflow context providing shared state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The agent's response as a <see cref="InvestigationObjective" />.</returns>
    [MessageHandler]
    public async ValueTask<InvestigationObjective> HandleInvestigationObjectiveAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(InvestigationObjective)}");


        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Gracefully falling back to {nameof(InvestigationObjective)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(InvestigationObjective)}."), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            InvestigationObjective result = await GenerateDecisionAsync(message, context, cancellationToken).ConfigureAwait(false);



            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(InvestigationObjective)}");

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

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(InvestigationObjective)} due to error.");
            return new InvestigationObjective();
        }
    }
}