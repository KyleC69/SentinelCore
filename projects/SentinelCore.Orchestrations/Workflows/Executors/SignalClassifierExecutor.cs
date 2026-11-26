// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SignalClassifierExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.Diagnostics;

using Newtonsoft.Json;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Exceptions;
using SentinelCore.Orchestrations.Infrastructure;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Generates an InvestigationObjective that defines the investigative goal for a case.
/// </summary>
/// <remarks>
///     Attributed with YieldsOutput and intended for use by the agent pipeline to produce a structured
///     InvestigationObjective for downstream processing.
/// </remarks>
[YieldsOutput(typeof(ChatMessage))]
[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
public sealed partial class SignalClassifierExecutor : Executor
{
    private AIAgent? _agent;
    private readonly ISentinelAgentFactory _agentfactory;

    private readonly ISystemReporter _reporter;








    public SignalClassifierExecutor(ISentinelAgentFactory agentFactory, ISystemReporter reporter) : base("SignalClassifierExecutor", ExecutorOutputPolicy.CreateExplicitYieldsOnly())
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentfactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));

    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public required string Name { get; init; }








    [MessageHandler]
    public async ValueTask<ChatMessage> GenerateInvestigationObjectiveAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {



        _agent = await _agentfactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentToolSelection.None, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, ChatResponseFormat.Json, token);
        Throw.IfNull(_agent);

        AgentSession session = await _agent.CreateSessionAsync(token);
        string classify = "The following signal should be classified and an InvestigationObjective should be generated based on its content. Provide as much information as you can on the signal, and be sure to respond with a Json object, no wrapping or additional commentary allowed.";

        // Use the AI agent to generate an initial InvestigationObjective from the message
        // Classifier will extrapolate the signal and generate an objective based on the message content.
        AgentResponse<InvestigationObjective> response = await _agent.RunAsync<InvestigationObjective>(classify, session, null, null, token);

        if (response.Result is not InvestigationObjective)
        {
            _reporter.ReportError($"Failed to generate InvestigationObjective from message: {message.Text}");
            // For now, let's assume a failure means we can't proceed.
            throw new SentinelCoreWorkflowException("Failed to generate InvestigationObjective.");
        }

        if (string.IsNullOrEmpty(response.Text))
        {
            _reporter.ReportError($"Generated InvestigationObjective is empty for message: {message.Text}");
            throw new SentinelCoreWorkflowException("Generated InvestigationObjective is empty.");
        }




        ChatMessage msg = new(ChatRole.User, response.Text);


        // await context.YieldOutputAsync(msg, token);
        await context.SendMessageAsync(msg, token);
        await context.SendMessageAsync(new TurnToken(true), cancellationToken: token);
        Debug.WriteLine($"Generated InvestigationObjective: {JsonConvert.SerializeObject(response.Result)}");
        Debug.WriteLine("");
        Debug.WriteLine("");
        Debug.WriteLine(msg.Text);
        Debug.WriteLine("");
        Debug.WriteLine("");
        Debug.WriteLine("End of Signal Classifier.");

        return null;
    }
}