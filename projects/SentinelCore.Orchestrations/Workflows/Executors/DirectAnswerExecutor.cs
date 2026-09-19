// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DirectAnswerExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;




/// <summary>
/// Executes a direct-answer workflow step by invoking an AIAgent to produce a textual response, reporting progress and
/// returning the agent's text output.
/// Consider a safety valve for human operator escalation if the agent fails to produce a valid response.
/// Consider implementing a retry mechanism for transient failures in agent execution.
/// Consider a safety filter to validate the agent's output before yielding it to the workflow context.
/// </summary>
/// <remarks>On failure, exceptions are reported to the reporter and rethrown. The executor calls agent.RunAsync
/// to obtain an AgentResponse, yields the resulting text to the workflow via IWorkflowContext.YieldOutputAsync, and
/// returns the same text.</remarks>
/// <param name="agent">Model-backed AIAgent used to generate the answer for the workflow step.</param>
/// <param name="reporter">ISystemReporter used to emit informational and error reports during execution.</param>
[YieldsOutput(typeof(string))]
public partial class DirectAnswerExecutor(AIAgent agent, ISystemReporter reporter) : Executor("DirectAnswer")
{
    //TODO: We need to bring in TheCore agent, and it's session, this agent interaction needs recall in future for conversational consistency


    /// <summary>
    /// Handles the direct answer workflow step by invoking an AIAgent to produce a textual response.
    /// </summary>
    /// <param name="input">The <see cref="SignalHypothesis"/> containing the prompt for the agent.</param>
    /// <param name="context">The <see cref="IWorkflowContext"/> for yielding output.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
    /// <exception cref="Exception">Thrown if any error occurs during agent execution or reporting.</exception>
    [MessageHandler]
    public async ValueTask HandleAnswerAsync(SignalHypothesis input, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        AgentResponse agResponse = null!;
        try
        {
            reporter.ReportInfo("Starting HandleAsync in DirectAnswerExecutor");
            Throw.IfNull(input, nameof(input));

            //If prompt did not carry forward get it from the state
            if (input.OrigPrompt is null)
            {
                var prompt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", cancellationToken);
            }

            // Simulate some processing logic
            agResponse = await agent.RunAsync(input.OrigPrompt, null, null, cancellationToken);
            if (agResponse == null)
            {
                await context.YieldOutputAsync("I am unable to provide a response at this time.", cancellationToken);
            }

        }
        catch (Exception ex)
        {
            // IMMEDIATELY isolate the step that failed
            reporter.ReportError($"[CRITICAL WORKFLOW ERROR] Failed at {this.ToString()}", ex);
            reporter.ReportError($"Exception Type: {ex.GetType().Name}", ex);
            reporter.ReportError($"Stack Trace: {ex.StackTrace}", ex);
            await context.YieldOutputAsync("An error occurred while processing your request.", cancellationToken);
        }

        reporter.ReportInfo("Finished HandleAsync in DirectAnswerExecutor");
        await context.YieldOutputAsync(agResponse.Text, cancellationToken);
    }







}