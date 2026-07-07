// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         DomainAgentFactory.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OllamaSharp;

using SentinelCore.Contracts;

using SentinelCoreLib.Application.Abstractions;




namespace SentinelCoreLib.Agents.Domain;





/// <summary>
///     Creates the reusable Domain Agent configured with a skill at invocation time.
/// </summary>
public sealed class DomainAgentFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<SentinelCoreSettings> _options;
    private readonly IToolRegistry _toolRegistry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainAgentFactory" /> class.
    /// </summary>
    public DomainAgentFactory(IOptions<SentinelCoreSettings> options, ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
    }








    /// <summary>
    ///     Creates a Domain Agent
    /// </summary>
    /// <param name="name">The skill/agent name.</param>
    /// <param name="toolNames">The tools included in this skill.</param>
    /// <param name="description">The skill description.</param>
    /// <returns>A configured Domain Agent.</returns>
    public AIAgent CreateAgent(string name, IList<AITool> toolNames, string description)
    {
        SentinelCoreSettings options = _options.Value;
        OllamaApiClient client = new(options.DomainModel.Endpoint, options.DomainModel.ModelId);
        LoggingChatClient chatClient = new(client, _loggerFactory.CreateLogger("DomainAgent"));
        IList<AITool> tools = toolNames;

        return new ChatClientAgent(chatClient: chatClient, instructions: $"""
                                                                          You are the {name} Domain Agent. {description}
                                                                          You are a participant in a magnetic orchestration workflow managed by the Manager.
                                                                          You receive a bounded task, use only the provided tools, and return a structured result.
                                                                          Do not reason beyond the task. Do not call other agents. Do not mutate case state.
                                                                          """, name: name, tools: tools, loggerFactory: _loggerFactory);
    }
}