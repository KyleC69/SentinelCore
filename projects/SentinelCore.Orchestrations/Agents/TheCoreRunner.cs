// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreRunner.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.Workflows;




namespace SentinelCore.Agents;





internal class TheCoreRunner(AIAgent agent) : Executor<SignalHypothesis, string>("TheCoreRunner")
{
    private AgentSession _session = agent.CreateSessionAsync().Result;








    public override async ValueTask<string> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken token)
    {


        ChatMessage msg = new(ChatRole.User, message.Hypothesis);


        // Access the agent if needed, for example:
        AgentResponse response = await agent.RunAsync(msg, _session, cancellationToken: token).ConfigureAwait(false);
        // return response;

        // For now, let's return a placeholder response.
        return response.Text;
    }
}