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
/// </summary>
/// <remarks>On failure, exceptions are reported to the reporter and rethrown. The executor calls agent.RunAsync
/// to obtain an AgentResponse, yields the resulting text to the workflow via IWorkflowContext.YieldOutputAsync, and
/// returns the same text.</remarks>
/// <param name="agent">Model-backed AIAgent used to generate the answer for the workflow step.</param>
/// <param name="reporter">ISystemReporter used to emit informational and error reports during execution.</param>
[YieldsOutput(typeof(string))]
public partial class DirectAnswerExecutor(AIAgent agent, ISystemReporter reporter) : Executor("DirectAnswer")
{


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

            // Simulate some processing logic
            agResponse = await agent.RunAsync(input.OrigPrompt, null, null, cancellationToken);

        }
        catch (Exception ex)
        {
            // IMMEDIATELY isolate the step that failed
            reporter.ReportError(ex, $"[CRITICAL WORKFLOW ERROR] Failed at {this.ToString()}");
            reporter.ReportError(ex, $"Exception Type: {ex.GetType().Name}");
            reporter.ReportError(ex, $"Stack Trace: {ex.StackTrace}");
            throw;
        }

        reporter.ReportInfo("Finished HandleAsync in DirectAnswerExecutor");
        await context.YieldOutputAsync(agResponse.Text, cancellationToken);
    }







}