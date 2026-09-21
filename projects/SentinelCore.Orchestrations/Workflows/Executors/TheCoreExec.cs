// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreExec.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     The Agent Executor is especially constructed to use an extended context tied to the life-cycle of the application.
///     It is created manually on first use and the session persists through turns.
///     TODO: research alternative persistence strategies and checkpointing
/// </summary>
/// <param name="agent"></param>
/// <param name="session"></param>
/// <param name="reporter"></param>
internal class TheCoreExec(AIAgent agent, AgentSession session, ISystemReporter reporter) : Executor<SignalHypothesis, ChatMessage>("TheCoreExec")
{

    public override async ValueTask<ChatMessage> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        try
        {

            // Log the received message
            reporter.ReportInfo($"Handling SignalHypothesis: {message.Hypothesis}");

            //Create chatmessage with hypothesis
            ChatMessage msg = new(ChatRole.User, message.Hypothesis);

            // Delegate execution to the internal executor implementation
            AgentRunOptions aro = new();

            AgentResponse result = await agent.RunAsync(msg, session, aro, cancellationToken).ConfigureAwait(false);

            // Log the result
            reporter.ReportInfo($"Execution completed with result: {result.Text}");

            ChatMessage outMsg = new(ChatRole.Assistant, result.Text);

            // The return value is auto-yielded as workflow output (non-void handler
            // return + WithOutputFrom registration), so no explicit yield here —
            // an explicit yield would duplicate the message in the chat.
            return outMsg; //send to next step
        }
        catch (Exception ex)
        {
            // Report the error
            reporter.ReportError("An error occurred while handling the SignalHypothesis.", ex);
            throw;
        }
    }
}
