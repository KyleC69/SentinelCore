// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DirectAnswerExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.agents;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
/// Executes a direct-answer workflow step by invoking an AIAgent to produce a textual response, reporting progress and
/// returning the agent's text output.
/// Consider a safety valve for human operator escalation if the agent fails to produce a valid response.
/// Consider implementing a retry mechanism for transient failures in agent execution.
/// Consider a safety filter to validate the agent's output before yielding it to the workflow context.
/// </summary>
/// <remarks>On failure, exceptions are reported to the reporter and a user-visible fallback message is yielded;
/// cancellation is re-thrown so cooperative cancellation propagates. The executor calls agent.RunAsync
/// to obtain an AgentResponse and yields the resulting text to the workflow via IWorkflowContext.YieldOutputAsync.</remarks>
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class DirectAnswerExecutor : Executor
{
    private readonly AIAgent _agent;
    private readonly AgentSession _session;
    private readonly ISystemReporter _reporter;

    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Initializes a new instance of the <see cref="DirectAnswerExecutor" /> class.
    /// </summary>
    /// <param name="agent">Model-backed AIAgent used to generate the answer for the workflow step.</param>
    /// <param name="session">The persistent agent session for conversational consistency.</param>
    /// <param name="reporter">ISystemReporter used to emit informational and error reports during execution.</param>
    public DirectAnswerExecutor(AIAgent agent, AgentSession session, ISystemReporter reporter) : base("DirectAnswer")
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    //TODO: We need to bring in TheCore agent, and it's session, this agent interaction needs recall in future for conversational consistency
    //Agent creation need to be moved to allow the customization of the agent inside the executor








    /// <summary>
    /// Handles the direct answer workflow step by invoking an AIAgent to produce a textual response.
    /// </summary>
    /// <param name="input">The <see cref="SignalHypothesis"/> containing the prompt for the agent.</param>
    /// <param name="context">The <see cref="IWorkflowContext"/> for yielding output.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
    /// <exception cref="OperationCanceledException">Re-thrown when execution is cancelled so cancellation propagates.</exception>
    /// <remarks>
    /// On a null agent response or an execution failure the executor yields a user-visible
    /// fallback message and returns early; it never falls through to a null response dereference.
    /// </remarks>
    [MessageHandler]
    public async ValueTask HandleAnswerAsync(SignalHypothesis input, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        try
        {
            _reporter.ReportInfo("Starting HandleAsync in DirectAnswerExecutor");

            // If the prompt did not carry forward on the hypothesis, recover it from
            // the shared workflow state written by the classifier step.
            string prompt = input.OrigPrompt;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken).ConfigureAwait(false) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                // Nothing to ask the agent — fail soft with a user-visible message.
                _reporter.ReportWarning("DirectAnswerExecutor had no prompt on the hypothesis or in shared state.");
                await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage("I am unable to provide a response at this time."), cancellationToken).ConfigureAwait(false);
                return;
            }

            var instructions = new ChatMessages();
            instructions.AddSystemMessage(AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S);
            instructions.AddSystemMessage("Using the mcp tools in your toolbelt answer the questions ask of you. Do not fabricate answers.");
            instructions.AddUserMessage(prompt);


            AgentResponse agResponse = await _agent.RunAsync(instructions, _session, null, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(agResponse.Text))
            {
                // Fail soft and stop — this previously fell through to agResponse.Text (NRE).
                _reporter.ReportWarning("DirectAnswerExecutor agent returned a null response.");
                await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage("I am unable to provide a response at this time."), cancellationToken).ConfigureAwait(false);
                return;
            }

            _reporter.ReportInfo("Finished HandleAsync in DirectAnswerExecutor");
            await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage(agResponse.Text), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            throw;
        }
        catch (Exception ex)
        {
            // IMMEDIATELY isolate the step that failed, surface a user-visible
            // message, and stop — this previously fell through to agResponse.Text (NRE).
            _reporter.ReportError($"[CRITICAL WORKFLOW ERROR] Failed at {this}", ex);
            _reporter.ReportError($"Exception Type: {ex.GetType().Name}", ex);
            _reporter.ReportError($"Stack Trace: {ex.StackTrace}", ex);
            await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage("An error occurred while processing your request."), cancellationToken).ConfigureAwait(false);
        }
    }



}
