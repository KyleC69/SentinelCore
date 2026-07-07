// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         DynamicAgentFactory.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OllamaSharp;

using SentinelCore.Contracts;

using SentinelCoreLib.Application;




namespace SentinelCoreLib.Agents.Dynamic;





/// <summary>
///     Creates dynamic (composite) agents on demand for cross-domain tasks.
/// </summary>
public sealed class DynamicAgentFactory
{
    private readonly string _domain;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<SentinelCoreSettings> _optionsProvider;








    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicAgentFactory" /> class.
    /// </summary>
    public DynamicAgentFactory(IOptions<SentinelCoreSettings> optionsProvider, ILoggerFactory loggerFactory)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _loggerFactory = loggerFactory;
        _domain = "dynamic";
    }








    /// <summary>
    ///     Creates a dynamic agent with the specified name, instructions, and tools.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="instructions">The system instructions.</param>
    /// <returns>The dynamic AI agent.</returns>
    public AIAgent Create(string name, string instructions, IReadOnlyList<string> toolNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        ArgumentNullException.ThrowIfNull(toolNames);

        SentinelCoreSettings options = _optionsProvider.Value;
        OllamaApiClient client = new(options.DomainModel.Endpoint, options.DomainModel.ModelId);
        LoggingChatClient chatClient = new(client, _loggerFactory.CreateLogger("DynamicAgent"));
        List<AITool> tools = ToolRegistry.GetToolByDomain(_domain)!.ToList();

        return new ChatClientAgent(chatClient: chatClient, instructions: instructions, name: name, tools: tools, loggerFactory: _loggerFactory);
    }
}