// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ManagerAgentFactory.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OllamaSharp;

using SentinelCore.Contracts;

using SentinelCoreLib.Agents.Domain;
using SentinelCoreLib.Agents.Dynamic;




namespace SentinelCoreLib.Agents.Manager;





/// <summary>
///     Creates the Manager agent that executes Core plans by delegating to domain and dynamic agents.
/// </summary>
public sealed class ManagerAgentFactory
{
    private readonly DomainAgentFactory _domainAgentFactory;
    private readonly DynamicAgentFactory _dynamicAgentFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<SentinelCoreSettings> _options;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ManagerAgentFactory" /> class.
    /// </summary>
    public ManagerAgentFactory(DomainAgentFactory domainAgentFactory, DynamicAgentFactory dynamicAgentFactory, IOptions<SentinelCoreSettings> options, ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _domainAgentFactory = domainAgentFactory ?? throw new ArgumentNullException(nameof(domainAgentFactory));
        _dynamicAgentFactory = dynamicAgentFactory ?? throw new ArgumentNullException(nameof(dynamicAgentFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }








    private static string BuildInstructions() => """
                                                 You are the SentinelCore Manager, a magnetic orchestration agent.
                                                 You receive a structured investigation plan from the Core agent.
                                                 Your job is to execute that plan inside the Agent Framework runtime by dispatching
                                                 the predefined Domain Agents and, when the plan calls for cross-domain work, the
                                                 dynamic composite agent.

                                                 Rules:
                                                 - Do not reason beyond the plan or invent new investigation steps.
                                                 - Delegate each plan step to the correct Domain Agent tool.
                                                 - For cross-domain steps, invoke the 'dynamic_agent' tool with a clear role,
                                                   combined toolbelt, and output schema.
                                                 - Collect structured results and synthesize them into a single structured response
                                                   returned to the Core.
                                                 - You do not own case lifecycle state. You do not write evidence directly. You only
                                                   return findings to the Core.
                                                 """;








    /// <summary>
    ///     Creates the manager agent that is responsible for spawning the appropritate domain agents (Magnetic Orchestration
    ///     Participants)
    /// </summary>
    /// <returns>The manager AI agent.</returns>
    public AIAgent Create()
    {
        SentinelCoreSettings options = _options.Value;
        OllamaApiClient client = new(options.ManagerModel.Endpoint, options.ManagerModel.ModelId);
        LoggingChatClient baseClient = new(client, _loggerFactory.CreateLogger("WorkflowManager"));


        ChatClientAgent manager = new(chatClient: baseClient, instructions: BuildInstructions(), name: "WorkflowManager", description: """
                                                                                                                                       Magnetic Orchestration Manager agent is responsible for executing the tasks given to him by The Core. 
                                                                                                                                       """, loggerFactory: _loggerFactory);

        manager.AsBuilder().UseLogging(loggerFactory: _loggerFactory).Build();
        return manager;

    }
}