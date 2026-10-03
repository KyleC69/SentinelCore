// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SafetyExecutorTests.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using System.Reflection;

using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.SafetyEngine;
using SentinelCore.Orchestrations.Workflows.Executors;
using SentinelCore.Orchestrations.Workflows.Helpers;




namespace SentinelCore.Tests;





[TestClass]
public class SafetyExecutorTests
{
    [TestMethod]
    public async Task HandleChatMessageAsync_AllowedMessageWithZeroScore_ShouldNotBeBlocked()
    {
        // Arrange
        var reporterMock = new Mock<ISystemReporter>();
        Exception? capturedException = null;
        reporterMock.Setup(r => r.ReportError(It.IsAny<string>(), It.IsAny<Exception>())).Callback<string, Exception>((m, e) => capturedException = e);

        var agentFactoryMock = new Mock<ISentinelAgentFactory>();
        var contextMock = new Mock<IWorkflowContext>();
        contextMock.Setup(c => c.YieldOutputAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());

        SafetyExecutor executor = new(reporterMock.Object, agentFactoryMock.Object, NullLoggerFactory.Instance);

        ChatMessage message = new(ChatRole.User, "Safe message");
        try
        {
            // Try to ensure AdditionalProperties is initialized
            if (message.AdditionalProperties == null)
            {
                PropertyInfo? prop = message.GetType().GetProperty("AdditionalProperties");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(message, new Dictionary<string, object?>());
                }
            }
        }
        catch
        {
            /* Ignore reflection errors */
        }

        // Act
        DetectionBoolResult result = await executor.HandleChatMessageAsync(message, contextMock.Object, CancellationToken.None);

        // Assert
        if (capturedException != null)
        {
            Assert.Fail($"Executor failed with exception: {capturedException.Message}\nStack trace: {capturedException.StackTrace}");
        }

        if (result.Prompt.Text.StartsWith("⚠️"))
        {
            Assert.Fail($"Executor failed with internal error: {result.Prompt.Text}");
        }

        // In DetectionBoolResult, IsTrue == true means blocked (as per SafetyExecutor implementation)
        Assert.IsFalse(result.IsTrue, "Message should not be flagged as blocked.");
        Assert.AreEqual("Safe message", result.Prompt.Text);

        // The original message should now have the safety tags if it passed through correctly
        Assert.IsTrue(result.Prompt.AdditionalProperties.ContainsKey("safetyResult"), "Safety result tag should be present on the message.");
        Assert.AreEqual("allowed", result.Prompt.AdditionalProperties["safetyResult"]);
    }








    [TestMethod]
    public async Task HandleChatMessageAsync_BlockedMessage_ShouldBeBlocked()
    {
        // Arrange
        Mock<ISystemReporter> reporterMock = new();
        Mock<ISentinelAgentFactory> agentFactoryMock = new();
        Mock<IWorkflowContext> contextMock = new();

        // Create a mock rule that always blocks
        Mock<ISafetyRule> mockRule = new();
        mockRule.Setup(r => r.Name).Returns("MockBlockRule");
        mockRule.Setup(r => r.EvaluateAsync(It.IsAny<SafetyEvaluationContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(SafetyRuleResult.Block("MockBlockRule", SafetySeverity.High, "Blocked by mock rule"));

        SafetyExecutor executor = new(reporterMock.Object, agentFactoryMock.Object, NullLoggerFactory.Instance, new[] { mockRule.Object });

        ChatMessage message = new(ChatRole.User, "Bad message");

        // Act
        DetectionBoolResult result = await executor.HandleChatMessageAsync(message, contextMock.Object, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsTrue, "Message should be flagged as blocked.");
        Assert.AreEqual("False", result.Prompt.AdditionalProperties["IsAllowed"]);
        Assert.AreEqual("blocked", result.Prompt.AdditionalProperties["safetyResult"]);
    }
}