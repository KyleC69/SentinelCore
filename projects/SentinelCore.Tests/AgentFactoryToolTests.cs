// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryToolTests.cs
// Author: Kyle L. Crowder
// Build Num:  110415



using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for the <see cref="SentinelAgentFactory" /> focusing on tool availability.
/// </summary>
[TestClass]
public sealed class AgentFactoryToolTests
{
    private Mock<IChatClientFactory> _chatClientFactoryMock = null!;
    private SentinelAgentFactory _factory = null!;
    private Mock<ILoggerFactory> _loggerFactoryMock = null!;
    private Mock<IMcpServerRegistry> _mcpServerRegistryMock = null!;
    private Mock<IModelResolver> _modelResolverMock = null!;
    private Mock<IPatternMatcher> _patternMatcherMock = null!;








    [TestMethod]
    public void CreateCoreAgent_IncludesLocalTools()
    {
        // Act
        AIAgent agent = _factory.CreateAgentAsync(AgentPreset.CoreAgent).GetAwaiter().GetResult();

        // Assert
        Assert.IsNotNull(agent, "Agent should not be null");

        // Check that the agent has tools
        // We access the agent's tools via the ChatOptions property (which is internal, so we use reflection)
        // Alternatively, we can check the agent's tool invocations? But we don't have a public way.
        // Since the agent is a ChatClientAgent, we can try to access its ChatOptions.
        // However, ChatOptions is not public. We'll use reflection for the test.

        Type chatClientAgentType = agent.GetType();
        PropertyInfo? chatOptionsProperty = chatClientAgentType.GetProperty("ChatOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(chatOptionsProperty, "ChatOptions property should exist");

        object? chatOptions = chatOptionsProperty.GetValue(agent);
        Assert.IsNotNull(chatOptions, "ChatOptions should not be null");

        // The ChatOptions type is Microsoft.Agents.AI.Chat.ChatOptions
        PropertyInfo? toolsProperty = chatOptions.GetType().GetProperty("Tools", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(toolsProperty, "Tools property should exist in ChatOptions");

        IList<AITool>? tools = (IList<AITool>)toolsProperty.GetValue(chatOptions);
        Assert.IsNotNull(tools, "Tools list should not be null");

        // We expect at least one tool from the local tools (MSDocsMcpServerTool)
        // We can check by type or by name.
        // Since we don't want to depend on the exact implementation, we just check that there is at least one tool.
        // However, note that the MCP server registry was mocked to return empty, so any tools must be local.
        Assert.IsTrue(tools.Count > 0, "Agent should have at least one tool");

        // Optionally, we can check for the specific tool we know about.
    }








    [TestInitialize]
    public void TestInitialize()
    {
        _chatClientFactoryMock = new Mock<IChatClientFactory>();
        _modelResolverMock = new Mock<IModelResolver>();
        _patternMatcherMock = new Mock<IPatternMatcher>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _mcpServerRegistryMock = new Mock<IMcpServerRegistry>();

        // Setup the logger factory to return a null logger
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        // Setup the model resolver to return a valid model profile for the Core tier
        ModelProfile modelProfile = new("test-model", "http://test.endpoint", 0.7f);
        _modelResolverMock.Setup(x => x.ResolveByTier(ModelTier.Core)).Returns(modelProfile);

        // Setup the MCP server registry to return no tools (so we can test local tools)
        _mcpServerRegistryMock.Setup(x => x.GetToolsFromCache()).Returns(new List<AITool>().AsReadOnly());
        _mcpServerRegistryMock.Setup(x => x.ToolCount).Returns(0);

        _factory = new SentinelAgentFactory(_chatClientFactoryMock.Object, _modelResolverMock.Object, _patternMatcherMock.Object, _loggerFactoryMock.Object, _mcpServerRegistryMock.Object);
    }
}