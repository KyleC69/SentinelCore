// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         EscalatedExecutorTests.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Agents.AI.Workflows;

using Moq;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Orchestrations.Workflows;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Tests.Workflows.Executors;





[TestClass]
public class EscalatedExecutorTests
{
    private EscalatedExecutor _executor;
    private Mock<ICaseFlowEngine> _mockCaseFlowEngine;
    private Mock<IWorkflowContext> _mockContext;
    private Mock<ISystemReporter> _mockReporter;








    [TestMethod]
    public async Task HandleChatMessageAsync_AdvanceCaseFails_StillYieldsEscalation()
    {
        // Arrange
        ChatMessage message = new(ChatRole.User, "Escalated Signal");
        Guid caseId = Guid.NewGuid();

        _mockContext.Setup(c => c.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", It.IsAny<CancellationToken>())).ReturnsAsync(caseId);

        _mockCaseFlowEngine.Setup(e => e.AdvanceCaseAsync(caseId, CaseStatus.Escalated, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Failure"));

        // Act
        await _executor.HandleChatMessageAsync(message, _mockContext.Object, CancellationToken.None);

        // Assert
        _mockReporter.Verify(r => r.ReportError(It.Is<string>(s => s.Contains("Failed to advance case")), It.IsAny<Exception>()), Times.Once);
        _mockContext.Verify(c => c.YieldOutputAsync(It.Is<ChatMessage>(m => m.Text.Contains("escalated for human review")), It.IsAny<CancellationToken>()), Times.Once);
    }








    [TestMethod]
    public async Task HandleChatMessageAsync_Canceled_PropagatesException()
    {
        // Arrange
        ChatMessage message = new(ChatRole.User, "Escalated Signal");
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
    public async Task HandleChatMessageAsync_NullMessage_YieldsErrorAndReturns()
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
    public async Task HandleChatMessageAsync_ValidMessage_AdvancesCaseAndYieldsEscalation()
    {
        // Arrange
        ChatMessage message = new(ChatRole.User, "Escalated Signal");
        Guid caseId = Guid.NewGuid();

        _mockContext.Setup(c => c.ReadStateAsync<Guid?>(WorkFlowStateKeys.CASE_ID, "SharedState", It.IsAny<CancellationToken>())).ReturnsAsync(caseId);

        // Act
        await _executor.HandleChatMessageAsync(message, _mockContext.Object, CancellationToken.None);

        // Assert
        _mockCaseFlowEngine.Verify(e => e.AdvanceCaseAsync(caseId, CaseStatus.Escalated, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.YieldOutputAsync(It.Is<ChatMessage>(m => m.Text.Contains("escalated for human review")), It.IsAny<CancellationToken>()), Times.Once);
        _mockReporter.Verify(r => r.ReportInfo(It.Is<string>(s => s.Contains("Completed successfully"))), Times.Once);
    }








    [TestInitialize]
    public void Setup()
    {
        _mockCaseFlowEngine = new Mock<ICaseFlowEngine>();
        _mockReporter = new Mock<ISystemReporter>();
        _mockContext = new Mock<IWorkflowContext>();

        _executor = new EscalatedExecutor(_mockCaseFlowEngine.Object, _mockReporter.Object);
    }
}