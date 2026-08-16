// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NewCaseExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Abstractions;
using SentinelCore.Cfe;
using SentinelCore.Exceptions;




namespace SentinelCore.Workflows.Executors;





/// <summary>
///     NON-Agent
///     An executor starts new case and publishes the CaseId to the context for other executors and also to UI/loggers
///     As a case moves through the pipeline the non-agent executors build onto the case currently being investigated
///     Each step is focused, clean, and deliberate. clear separation enforced
/// </summary>
public sealed class NewCaseExecutor(ICaseFlowEngine caseEng, ISystemReporter reporter) : Executor<SignalHypothesis, SignalHypothesis>("NewCase")
{

    // This field is currently unused; make it nullable to silence the warning.
    private const string SharedState = "SharedState";








    public override async ValueTask<SignalHypothesis> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken token = default)
    {
        try
        {
            reporter.DebugMsg("New case starting now.");
            string? prmpt = await context.ReadStateAsync<string>(WorkFlowStateKeys.PROMPT, "SharedState", token).ConfigureAwait(false);
            Guid caseId = await caseEng.CreateCaseAsync(new Signal(message.Hypothesis ?? "No Hypothesis Entered", "User"), token).ConfigureAwait(false);

            //Starts new case, saves caseid to context,
            //Log action and publish to UI
            reporter.ReportInfo($"New case created with ID: {caseId}.");


            // Set caseid so it can be picked up by future steps.
            await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, caseId, SharedState, token).ConfigureAwait(false);
            await context.QueueStateUpdateAsync(WorkFlowStateKeys.SIGNAL_HYPOTHESIS, message, SharedState, token).ConfigureAwait(false);

            await context.YieldOutputAsync(message).ConfigureAwait(false); //Bubble caseid to output



        }
        catch (Exception e)
        {
            reporter.ReportError(e, $"Failure during new case creation executor: {Id}");

        }



        //pass original message through
        return message;
    }








    private async Task HandleValidationFailure(string errorMessage)
    {
        // Update the case status to "Failed" and append notes
        // To get to this point we have gone through several data validation steps, we should bump this up to ops for review if we get an error.
        await caseEng.AdvanceCaseAsync(Guid.Empty, CaseStatus.AwaitingInput, CancellationToken.None).ConfigureAwait(false);
        reporter.ReportError(new SentinelCaseEngineException("Failure during model output verification"), "Failed llm validation");

        // Optionally, you can add more logic here to log additional details or perform other actions
    }








    private bool IsHypothesisValid(SignalHypothesis hypo)
    {
        return !string.IsNullOrWhiteSpace(hypo.Category) && !string.IsNullOrWhiteSpace(hypo.Hypothesis) && !string.IsNullOrWhiteSpace(hypo.Reasoning) && hypo.InitialConfidenceScore > 0;
    }
}