// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SafetyEngineTests.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging.Abstractions;

using SentinelCore.Orchestrations.SafetyEngine;




namespace SentinelCore.Tests;





[TestClass]
public sealed class SafetyEngineAgentTests
{

    [TestMethod]
    public void CreateBlockedResponseUpdate_UsesDefaultWhenNoCustomMessage()
    {
        // Arrange
        var rules = Array.Empty<ISafetyRule>();
        SafetyEngineAgent agent = new(rules, NullLogger<SafetyEngineAgent>.Instance, SafetyEngineOptions.Default);

        SafetyEvaluationResult eval = new(false, new List<SafetyRuleResult> { SafetyRuleResult.Block("r", SafetySeverity.Medium, "reason") }, SafetySeverity.Medium, "blocked", SafetyRuleResult.Block("r", SafetySeverity.Medium, "reason"));

        // Act
        AgentResponseUpdate update = agent.CreateBlockedResponseUpdate(eval);

        // Assert
        Assert.AreEqual(ChatRole.Assistant, update.Role);
        Assert.IsTrue(update.Text.Contains("Request blocked by safety policy"));
    }








    [TestMethod]
    public void CreateBlockedResponse_UsesCustomBlockedMessage_WhenProvided()
    {
        // Arrange
        ISafetyRule[] rules = Array.Empty<ISafetyRule>();
        string customMsg = "Custom blocked message.";
        SafetyEngineOptions opts = SafetyEngineOptions.Default with { BlockedResponseMessage = customMsg };
        SafetyEngineAgent agent = new(rules, NullLogger<SafetyEngineAgent>.Instance, opts);

        SafetyEvaluationResult eval = new(false, new List<SafetyRuleResult> { SafetyRuleResult.Block("r", SafetySeverity.High, "x") }, SafetySeverity.High, "blocked", SafetyRuleResult.Block("r", SafetySeverity.High, "x"));

        // Act
        AgentResponse resp = agent.CreateBlockedResponse(eval);

        // Assert - message content should contain the custom message
        Assert.IsNotNull(resp);
        Assert.IsTrue(resp.Text.Contains(customMsg));
    }








    [TestMethod]
    public async Task EvaluateRulesAsync_Allows_WhenAllRulesAllow()
    {
        // Arrange
        List<ISafetyRule> rules = new() { new TestRule("r1", (_, _) => Task.FromResult(SafetyRuleResult.Allow("r1"))), new TestRule("r2", (_, _) => Task.FromResult(SafetyRuleResult.Allow("r2"))) };

        SafetyEngineAgent agent = new(rules, NullLogger<SafetyEngineAgent>.Instance);

        List<ChatMessage> messages = new() { new(ChatRole.User, "Hello") };
        SafetyEvaluationContext ctx = new(messages);

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

        List<ISafetyRule> rules = new()
        {
                new TestRule("blocker", (_, _) => Task.FromResult(SafetyRuleResult.Block("blocker", SafetySeverity.Critical, "blocked"))),
                new TestRule("should-not-run", (_, _) =>
                {
                    secondExecuted = true;
                    return Task.FromResult(SafetyRuleResult.Allow("should-not-run"));
                })
        };

        SafetyEngineAgent agent = new(rules, NullLogger<SafetyEngineAgent>.Instance, SafetyEngineOptions.Default with { StopOnFirstBlock = true });

        SafetyEvaluationContext ctx = new(new List<ChatMessage> { new(ChatRole.User, "Bad") });

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
        List<ISafetyRule> rules = new() { new TestRule("exploder", (_, _) => throw new InvalidOperationException("boom")), new TestRule("never-run", (_, _) => Task.FromResult(SafetyRuleResult.Allow("never-run"))) };

        SafetyEngineOptions opts = SafetyEngineOptions.Default with { TreatRuleErrorsAsBlocks = true, StopOnFirstBlock = true };
        SafetyEngineAgent agent = new(rules, NullLogger<SafetyEngineAgent>.Instance, opts);

        SafetyEvaluationContext ctx = new(new List<ChatMessage> { new(ChatRole.User, "Test") });

        // Act
        SafetyEvaluationResult result = await agent.EvaluateRulesAsync(ctx, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsAllowed);
        Assert.IsNotNull(result.BlockingResult);
        Assert.AreEqual("exploder", result.BlockingResult!.RuleName);
        Assert.IsTrue(result.BlockingResult.Reason.Contains("Rule evaluation failed"));
    }








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



        public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default) => _impl(context, cancellationToken);



        public string Name { get; }
    }
}