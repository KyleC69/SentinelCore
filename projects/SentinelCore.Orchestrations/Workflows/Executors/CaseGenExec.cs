// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseGenExec.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.CaseEngine;




namespace SentinelCore.Workflows.Executors;





/// <summary>
///     Quick case generation based on current environment
/// </summary>
internal class CaseGenExec(ICaseGenerator generator, ISystemReporter reporter, ICaseFlowEngine flowEngine) : Executor<SuppressionDecision, string>("CaseGen")
{

    public override async ValueTask<string> HandleAsync(SuppressionDecision message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        reporter.ReportInfo("Starting CaseGenExec");

        if (string.IsNullOrEmpty(message.Prompt))
        {
            throw new InvalidOperationException("SuppressionDecision.Prompt cannot be null or empty.");
        }

        try
        {



            AIAgent agent = await generator.GetAIAgentAsync().ConfigureAwait(false);
            AgentSession _session = await agent.CreateSessionAsync(cancellationToken);




            AgentResponse response = await agent.RunAsync(message.Prompt, _session, cancellationToken: cancellationToken);
            await context.SendMessageAsync(message.Prompt, cancellationToken: cancellationToken);
            return message.Prompt;
        }
        catch (Exception ex)
        {
            reporter.ReportError(ex, "CaseGenExec failed to generate case.");
            return "Failed to generate a case and had to exit the workflow. Please check the logs for more information.";
        }
    }
}