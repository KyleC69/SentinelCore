// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         DirectAnswerExecutorTests.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Agents.AI.Workflows;

using Moq;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Tests.Workflows.Executors;





[TestClass]
public class DirectAnswerExecutorTests
{
    private DirectAnswerExecutor _executor;
    private Mock<AIAgent> _mockAgent;
    private Mock<IWorkflowContext> _mockContext;
    private Mock<ISystemReporter> _mockReporter;
    private Mock<AgentSession> _mockSession;








    [TestMethod]
    public async Task HandleAnswerAsync_AgentNotTheCore_YieldsFallbackAndReturns()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        _mockAgent.Setup(a => a.Name).Returns("WrongAgent");

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("An internal error prevented me from providing a response.", result!.Text);
        _mockReporter.Verify(r => r.ReportError(It.Is<string>(s => s.Contains("Agent is null"))), Times.Once);
    }








    [TestMethod]
    public async Task HandleAnswerAsync_AgentReturnsEmpty_YieldsFallback()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        _mockContext.Setup(c => c.ReadStateAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("Valid Prompt");

        Mock<AgentResponse> agentResponse = new();
        agentResponse.Setup(r => r.Text).Returns("");
        _mockAgent.Setup(a => a.RunAsync(It.IsAny<ChatMessages>(), It.IsAny<AgentSession>(), It.IsAny<AgentRunOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(agentResponse.Object);

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I am unable to provide a response at this time.", result!.Text);
        _mockReporter.Verify(r => r.ReportWarning(It.Is<string>(s => s.Contains("returned a null response"))), Times.Once);
    }








    [TestMethod]
    public async Task HandleAnswerAsync_Canceled_PropagatesException()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        try
        {
            await _executor.HandleAnswerAsync(input, _mockContext.Object, cts.Token);
            Assert.Fail("Should have thrown OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Success
        }
    }








    [TestMethod]
    public async Task HandleAnswerAsync_Exception_YieldsFallback()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        _mockContext.Setup(c => c.ReadStateAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("Valid Prompt");

        _mockAgent.Setup(a => a.RunAsync(It.IsAny<ChatMessages>(), It.IsAny<AgentSession>(), It.IsAny<AgentRunOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Agent Crash"));

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("An error occurred while processing your request.", result!.Text);
        _mockReporter.Verify(r => r.ReportError(It.Is<string>(s => s.Contains("[CRITICAL WORKFLOW ERROR]")), It.IsAny<Exception>()), Times.Once);
    }








    [TestMethod]
    public async Task HandleAnswerAsync_NoPromptInState_YieldsFallbackAndReturns()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        _mockContext.Setup(c => c.ReadStateAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string)null!);

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I am unable to provide a response at this time.", result!.Text);
        _mockReporter.Verify(r => r.ReportWarning(It.Is<string>(s => s.Contains("had no prompt"))), Times.Once);
    }








    [TestMethod]
    public async Task HandleAnswerAsync_NullInput_YieldsFallbackAndReturns()
    {
        // Arrange
        ChatMessage? input = null;

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input!, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I am unable to process your request due to missing information.", result!.Text);
        _mockContext.Verify(c => c.YieldOutputAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockReporter.Verify(r => r.ReportWarning(It.Is<string>(s => s.Contains("received null input"))), Times.Once);
    }








    [TestMethod]
    public async Task HandleAnswerAsync_ValidFlow_ReturnsAgentText()
    {
        // Arrange
        ChatMessage input = new(ChatRole.User, "Hello");
        string expectedText = "This is the answer";

        _mockContext.Setup(c => c.ReadStateAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("Valid Prompt");

        Mock<AgentResponse> agentResponse = new();
        agentResponse.Setup(r => r.Text).Returns(expectedText);
        _mockAgent.Setup(a => a.RunAsync(It.IsAny<ChatMessages>(), It.IsAny<AgentSession>(), It.IsAny<AgentRunOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(agentResponse.Object);

        // Note: The actual method signature in DirectAnswerExecutor.cs is RunAsync(instructions, _session, null, cancellationToken)
        // Need to ensure the mock matches exactly if using strict mocks, but default is loose.
        // However, the provided code says: _agent.RunAsync(instructions, _session, null, cancellationToken)
        // Let's adjust the setup to match the likely signature.
        _mockAgent.Setup(a => a.RunAsync(It.IsAny<ChatMessages>(), It.IsAny<AgentSession>(), It.IsAny<AgentRunOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(agentResponse.Object);

        // Act
        ChatMessage? result = await _executor.HandleAnswerAsync(input, _mockContext.Object, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedText, result!.Text);
        _mockContext.Verify(c => c.YieldOutputAsync(It.Is<ChatMessage>(m => m.Text == expectedText), It.IsAny<CancellationToken>()), Times.Once);
        _mockReporter.Verify(r => r.ReportInfo(It.Is<string>(s => s.Contains("successfully"))), Times.Once);
    }








    [TestInitialize]
    public void Setup()
    {
        _mockAgent = new Mock<AIAgent>();
        _mockAgent.Setup(a => a.Name).Returns("TheCore");

        _mockSession = new Mock<AgentSession>();
        _mockReporter = new Mock<ISystemReporter>();
        _mockContext = new Mock<IWorkflowContext>();

        _executor = new DirectAnswerExecutor(_mockAgent.Object, _mockSession.Object, _mockReporter.Object);
    }
}