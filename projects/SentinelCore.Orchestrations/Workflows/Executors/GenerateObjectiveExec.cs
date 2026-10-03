// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         GenerateObjectiveExec.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Generates an InvestigationObjective that defines the investigative goal for a case.
/// </summary>
/// <remarks>
///     Attributed with YieldsOutput and intended for use by the agent pipeline to produce a structured
///     InvestigationObjective for downstream processing.
/// </remarks>
[YieldsOutput(typeof(InvestigationObjective))]
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class GenerateObjectiveExec : Executor
{
    private readonly AIAgent _agent;

    private readonly ISystemReporter _reporter;
    private readonly AgentSession _sentinelCoreSession;








    public GenerateObjectiveExec(AIAgent coreAgent, AgentSession sentinelCoreSession, ISystemReporter reporter) : base("GenerateObjectExec")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agent = coreAgent ?? throw new ArgumentNullException(nameof(coreAgent));
        _sentinelCoreSession = sentinelCoreSession ?? throw new ArgumentNullException(nameof(sentinelCoreSession));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    public async ValueTask<ChatMessage> GenerateInvestigationObjectiveAsync(ChatMessage? message, IWorkflowContext context, CancellationToken token)
    {
        if (message == null || string.IsNullOrEmpty(message.Text))
        {
            throw new ArgumentNullException(nameof(message));
        }


        // Build the instruction set for the AI agent. This includes the platform domain,
        // the core Sentinel instructions, and specific instructions for generating an objective.
        // This ensures the agent understands its role and the task at hand.
        ChatMessages instructionMessages = InstructionLayerBuilder.Build(AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S, AgentInstructionConstants.SENTINEL_CORE_INSTRUCTIONS, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN);

        // Combine the user's message with the constructed instructions.
        // The user message is typically the last in the sequence, providing the specific input
        // for the objective generation task.
        ChatMessages combinedMessages = new(instructionMessages)
        {
                message // Add the original message from the context
        };

        // Use the AI agent to generate an initial InvestigationObjective from the message
        // This is a placeholder; actual generation would involve prompting the AI agent.
        AgentResponse<InvestigationObjective> objective = await _agent.RunAsync<InvestigationObjective>(combinedMessages, _sentinelCoreSession, null, null, token);

        if (objective == null)
        {
            _reporter.ReportError($"Failed to generate InvestigationObjective from message: {message.Text}");
            // Depending on requirements, you might throw an exception or return a default/null objective.
            // For now, let's assume a failure means we can't proceed.
            throw new InvalidOperationException("Failed to generate InvestigationObjective.");
        }

        await context.QueueStateUpdateAsync(WorkFlowStateKeys.CASE_ID, "", "SharedState", token);

        await context.YieldOutputAsync(objective.Messages[0], token);
        // Return the enriched InvestigationObjective
        return objective.Messages[0];
    }








    [MessageHandler]
    public async ValueTask<ChatMessage> HandleChatMessage(NextStep step, IWorkflowContext context, CancellationToken token)
    {
        Throw.IfNullOrEmpty(step.ToString());

        ChatMessage? prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState", token);



        ChatMessage modelResponse = await GenerateInvestigationObjectiveAsync(prompt, context, token);

        return modelResponse;
    }
}