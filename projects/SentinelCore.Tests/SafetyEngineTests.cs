using Microsoft.Extensions.Logging.Abstractions;

using SentinelCore.Orchestrations.SafetyEngine;




namespace SentinelCore.Tests;



[TestClass]
public sealed class SafetyEngineAgentTests
{
    // Minimal test helper rule that can return a result or throw.
    private sealed class TestRule : ISafetyRule
    {
        private readonly Func<SafetyEvaluationContext, CancellationToken, Task<SafetyRuleResult>> _impl;

        public TestRule(string name, Func<SafetyEvaluationContext, CancellationToken, Task<SafetyRuleResult>> impl)
        {
            Name = name;
            Description = $"Test rule {name}";
            _impl = impl;
        }

        public string Description { get; }
        public string Name { get; }

        public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
            => _impl(context, cancellationToken);
    }

    [TestMethod]
    public async Task EvaluateRulesAsync_Allows_WhenAllRulesAllow()
    {
        // Arrange
        var rules = new List<ISafetyRule>
        {
            new TestRule("r1", (_, _) => Task.FromResult(SafetyRuleResult.Allow("r1"))),
            new TestRule("r2", (_, _) => Task.FromResult(SafetyRuleResult.Allow("r2")))
        };

        var agent = new SafetyEngineAgent(rules, NullLogger<SafetyEngineAgent>.Instance);

        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var ctx = new SafetyEvaluationContext(messages);

        // Act
        SafetyEvaluationResult result = await agent.EvaluateRulesAsync(ctx, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsAllowed);
        Assert.AreEqual(SafetySeverity.None, result.HighestSeverity);
        Assert.IsTrue(result.RuleResults.Any(r => r.RuleName == "r1"));
        Assert.IsTrue(result.RuleResults.Any(r => r.RuleName == "r2"));
    }

    [TestMethod]
    public async Task EvaluateRulesAsync_StopsOnFirstBlock_WhenConfigured()
    {
        // Arrange
        bool secondExecuted = false;

        var rules = new List<ISafetyRule>
        {
            new TestRule("blocker", (_, _) => Task.FromResult(SafetyRuleResult.Block("blocker", SafetySeverity.Critical, "blocked"))),
            new TestRule("should-not-run", (_, _) =>
            {
                secondExecuted = true;
                return Task.FromResult(SafetyRuleResult.Allow("should-not-run"));
            })
        };

        var agent = new SafetyEngineAgent(rules, NullLogger<SafetyEngineAgent>.Instance, SafetyEngineOptions.Default with { StopOnFirstBlock = true });

        var ctx = new SafetyEvaluationContext(new List<ChatMessage> { new(ChatRole.User, "Bad") });

        // Act
        SafetyEvaluationResult result = await agent.EvaluateRulesAsync(ctx, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsAllowed);
        Assert.IsNotNull(result.BlockingResult);
        Assert.AreEqual("blocker", result.BlockingResult!.RuleName);
        Assert.IsFalse(secondExecuted, "Second rule should not have been executed because StopOnFirstBlock == true");
    }

    [TestMethod]
    public async Task EvaluateRulesAsync_TreatsRuleExceptionsAsBlocks_WhenConfigured()
    {
        // Arrange
        var rules = new List<ISafetyRule>
        {
            new TestRule("exploder", (_, _) => throw new InvalidOperationException("boom")),
            new TestRule("never-run", (_, _) => Task.FromResult(SafetyRuleResult.Allow("never-run")))
        };

        var opts = SafetyEngineOptions.Default with { TreatRuleErrorsAsBlocks = true, StopOnFirstBlock = true };
        var agent = new SafetyEngineAgent(rules, NullLogger<SafetyEngineAgent>.Instance, opts);

        var ctx = new SafetyEvaluationContext(new List<ChatMessage> { new(ChatRole.User, "Test") });

        // Act
        SafetyEvaluationResult result = await agent.EvaluateRulesAsync(ctx, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsAllowed);
        Assert.IsNotNull(result.BlockingResult);
        Assert.AreEqual("exploder", result.BlockingResult!.RuleName);
        Assert.IsTrue(result.BlockingResult.Reason.Contains("Rule evaluation failed"));
    }

    [TestMethod]
    public void CreateBlockedResponse_UsesCustomBlockedMessage_WhenProvided()
    {
        // Arrange
        var rules = Array.Empty<ISafetyRule>();
        var customMsg = "Custom blocked message.";
        var opts = SafetyEngineOptions.Default with { BlockedResponseMessage = customMsg };
        var agent = new SafetyEngineAgent(rules, NullLogger<SafetyEngineAgent>.Instance, opts);

        var eval = new SafetyEvaluationResult(false, new List<SafetyRuleResult> { SafetyRuleResult.Block("r", SafetySeverity.High, "x") }, SafetySeverity.High, "blocked", SafetyRuleResult.Block("r", SafetySeverity.High, "x"));

        // Act
        AgentResponse resp = agent.CreateBlockedResponse(eval);

        // Assert - message content should contain the custom message
        Assert.IsNotNull(resp);
        Assert.IsTrue(resp.Text.Contains(customMsg));
    }

    [TestMethod]
    public void CreateBlockedResponseUpdate_UsesDefaultWhenNoCustomMessage()
    {
        // Arrange
        var rules = Array.Empty<ISafetyRule>();
        var agent = new SafetyEngineAgent(rules, NullLogger<SafetyEngineAgent>.Instance, SafetyEngineOptions.Default);

        var eval = new SafetyEvaluationResult(false, new List<SafetyRuleResult> { SafetyRuleResult.Block("r", SafetySeverity.Medium, "reason") }, SafetySeverity.Medium, "blocked", SafetyRuleResult.Block("r", SafetySeverity.Medium, "reason"));

        // Act
        AgentResponseUpdate update = agent.CreateBlockedResponseUpdate(eval);

        // Assert
        Assert.AreEqual(ChatRole.Assistant, update.Role);
        Assert.IsTrue(update.Text.Contains("Request blocked by safety policy"));
    }
}
