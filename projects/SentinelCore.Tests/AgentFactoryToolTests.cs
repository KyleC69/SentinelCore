// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryToolTests.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for the <see cref="SentinelAgentFactory" /> focusing on tool availability.
/// </summary>
[TestClass]
public sealed class AgentFactoryToolTests
{
    private const string PromptText = "ping";
    private const string RegistryToolName = "registry_noop";

    private FakeChatClient _chatClient = null!;
    private Mock<IChatClientFactory> _chatClientFactoryMock = null!;
    private SentinelAgentFactory _factory = null!;
    private Mock<ILoggerFactory> _loggerFactoryMock = null!;
    private Mock<IMcpServerRegistry> _mcpServerRegistryMock = null!;
    private Mock<IModelResolver> _modelResolverMock = null!;
    private Mock<IPatternMatcher> _patternMatcherMock = null!;








    /// <summary>
    ///     Verifies that the tools discovered by the MCP registry reach the model call issued by a Core
    ///     agent instead of being dropped while the agent is built.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateCoreAgent_AttachesRegistryToolsToModelCallAsync()
    {
        // Arrange
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.CoreAgent);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions? options = _chatClient.LastOptions;
        Assert.IsNotNull(options, "Running the agent must issue a chat request to the model provider.");
        Assert.IsNotNull(options.Tools, "The chat request must carry the agent's tool definitions.");
        Assert.IsTrue(options.Tools.Any(tool => tool.Name == RegistryToolName), $"The registry tool '{RegistryToolName}' never reached the model call, so the agent could not invoke it.");
    }








    /// <summary>
    ///     Configures a factory whose external collaborators are deterministic test doubles: a chat client
    ///     that records what it was asked to send, and a registry that exposes exactly one tool.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _chatClient = new FakeChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, "pong")));

        _chatClientFactoryMock = new Mock<IChatClientFactory>();
        _chatClientFactoryMock.Setup(factory => factory.CreateChatClient(It.IsAny<ModelProfile>())).Returns(_chatClient);

        _modelResolverMock = new Mock<IModelResolver>();
        _modelResolverMock.Setup(resolver => resolver.ResolveByTier(It.IsAny<ModelTier>())).Returns(new ModelProfile("test-model", "http://test.endpoint", 0.7f));

        _patternMatcherMock = new Mock<IPatternMatcher>();

        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        _mcpServerRegistryMock = new Mock<IMcpServerRegistry>();
        _mcpServerRegistryMock.Setup(x => x.GetToolsFromCache()).Returns(new List<AITool> { AIFunctionFactory.Create(() => "ok", RegistryToolName) }.AsReadOnly());
        _mcpServerRegistryMock.Setup(x => x.ToolCount).Returns(1);

        _factory = new SentinelAgentFactory(_chatClientFactoryMock.Object, _modelResolverMock.Object, _patternMatcherMock.Object, _loggerFactoryMock.Object, _mcpServerRegistryMock.Object);
    }
}