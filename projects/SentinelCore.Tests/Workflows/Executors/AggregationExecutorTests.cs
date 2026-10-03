// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AggregationExecutorTests.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Agents.AI.Workflows;

using Moq;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Tests.Workflows.Executors;





[TestClass]
public class AggregationExecutorTests
{
    private AggregationExecutor _executor;
    private Mock<IWorkflowContext> _mockContext;
    private Mock<ISystemReporter> _mockReporter;








    [TestMethod]
    public async Task HandleChatMessageAsync_Canceled_PropagatesException()
    {
        // Arrange
        ChatMessage message = new(ChatRole.User, "Aggregated Evidence");
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        try
        {
            await _executor.HandleChatMessageAsync(message, _mockContext.Object, cts.Token);
            Assert.Fail("Should have thrown OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Success
        }
    }








    [TestMethod]
    public async Task HandleChatMessageAsync_NullMessage_YieldsError()
    {
        // Arrange
        ChatMessage? message = null;

        // Act
        await _executor.HandleChatMessageAsync(message!, _mockContext.Object, CancellationToken.None);

        // Assert
        _mockContext.Verify(c => c.YieldOutputAsync(It.Is<ChatMessage>(m => m.Text.Contains("Input message was null")), It.IsAny<CancellationToken>()), Times.Once);
        _mockReporter.Verify(r => r.ReportError(It.Is<string>(s => s.Contains("Input message was null")), It.IsAny<Exception>()), Times.Once);
    }








    [TestMethod]
    public async Task HandleChatMessageAsync_ValidMessage_LogsProcessing()
    {
        // Arrange
        ChatMessage message = new(ChatRole.User, "Aggregated Evidence");

        // Act
        await _executor.HandleChatMessageAsync(message, _mockContext.Object, CancellationToken.None);

        // Assert
        _mockReporter.Verify(r => r.ReportInfo(It.Is<string>(s => s.Contains("Completed successfully"))), Times.Once);
    }








    [TestInitialize]
    public void Setup()
    {
        _mockReporter = new Mock<ISystemReporter>();
        _mockContext = new Mock<IWorkflowContext>();
        _executor = new AggregationExecutor(_mockReporter.Object);
    }
}