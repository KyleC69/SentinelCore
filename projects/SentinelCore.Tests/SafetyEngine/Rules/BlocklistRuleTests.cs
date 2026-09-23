// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         BlocklistRuleTests.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Orchestrations.SafetyEngine;
using SentinelCore.Orchestrations.SafetyEngine.Rules;




namespace SentinelCore.Tests;





[TestClass]
public sealed class BlocklistRuleTests
{
    private BlocklistRule? _rule;



    private BlocklistRule Rule
    {
        get => _rule ?? throw new InvalidOperationException("BlocklistRule test fixture was not initialized.");
    }








    [TestMethod]
    public void BlocklistRule_Initialization_CustomArguments_SetsCorrectValues()
    {
        // Arrange
        BlocklistRule customRule = new("CustomRule", SafetySeverity.Medium, "Custom check");

        // Assert
        Assert.AreEqual("CustomRule", customRule.Name);
        Assert.AreEqual(SafetySeverity.Medium, customRule.Severity);
        Assert.AreEqual("Custom check", customRule.Description);
    }








    [TestMethod]
    public void BlocklistRule_Initialization_SetsCorrectDefaults()
    {
        // Arrange
        BlocklistRule rule = Rule;

        // Assert
        Assert.AreEqual("DefaultBlocklistRule", rule.Name);
        Assert.AreEqual(SafetySeverity.High, rule.Severity);
        Assert.AreEqual("Blocks prompts containing blocklisted terms or phrases.", rule.Description);
    }








    private static SafetyEvaluationContext CreateContext(string text)
    {
        IReadOnlyList<ChatMessage> messages = new List<ChatMessage> { new(ChatRole.User, text) };

        return new SafetyEvaluationContext(messages);
    }








    [TestMethod]
    public async Task EvaluateAsync_CleanContext_ShouldPass()
    {
        // Arrange
        BlocklistRule rule = Rule;
        SafetyEvaluationContext context = CreateContext("Please describe the process of migrating data from SQL Server to Azure Cosmos DB.");

        // Act
        SafetyRuleResult result = await rule.EvaluateAsync(context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(SafetyAction.Allow, result.Action);
        Assert.AreEqual(SafetySeverity.None, result.Severity);
        Assert.AreEqual(0, result.Score);
    }








    [TestMethod]
    public async Task EvaluateAsync_ExceedsBlockThreshold_ShouldBlock()
    {
        // Arrange
        BlocklistRule rule = Rule;
        SafetyEvaluationContext context = CreateContext("I want to kill myself and deploy ransomware and bomb the building.");

        // Act
        SafetyRuleResult result = await rule.EvaluateAsync(context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(SafetyAction.Block, result.Action);
        Assert.AreEqual(SafetySeverity.Critical, result.Severity);
        Assert.IsTrue(result.Score >= 15, $"Expected cumulative score >= 15 but got {result.Score}.");
        Assert.IsTrue(result.MatchedIndicators.Count >= 3);
    }








    [TestMethod]
    public async Task EvaluateAsync_ImmediateReviewTerm_ShouldWarn()
    {
        // Arrange
        BlocklistRule rule = Rule;
        SafetyEvaluationContext context = CreateContext("I want to kill myself today.");

        // Act
        SafetyRuleResult result = await rule.EvaluateAsync(context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(SafetyAction.Warn, result.Action);
        Assert.AreEqual(SafetySeverity.Medium, result.Severity);
        Assert.AreEqual(5, result.Score);
        Assert.IsTrue(result.MatchedIndicators.Count > 0);
    }








    [TestMethod]
    public async Task EvaluateAsync_MultipleModerateMatches_ShouldWarn()
    {
        // Arrange
        BlocklistRule rule = Rule;
        SafetyEvaluationContext context = CreateContext("Please steal the credentials and bypass monitoring so we can evade detection.");

        // Act
        SafetyRuleResult result = await rule.EvaluateAsync(context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(SafetyAction.Warn, result.Action);
        Assert.AreEqual(SafetySeverity.High, result.Severity);
        Assert.IsTrue(result.Score >= 10, $"Expected cumulative score >= 10 but got {result.Score}.");
        Assert.IsTrue(result.MatchedIndicators.Count >= 2);
    }








    [TestMethod]
    public async Task EvaluateAsync_WithExplicitBlocklist_ShouldBlockConfiguredTerm()
    {
        // Arrange
        BlocklistRule customRule = new("CustomRule", new[] { "alpha threat", "beta threat" }, SafetySeverity.Medium, "Custom check");
        SafetyEvaluationContext context = CreateContext("Please assess the beta threat in this prompt.");

        // Act
        SafetyRuleResult result = await customRule.EvaluateAsync(context, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(SafetyAction.Warn, result.Action);
        Assert.AreEqual(SafetySeverity.Medium, result.Severity);
        Assert.AreEqual(2, result.Score);
    }








    [TestInitialize]
    public void Setup()
    {
        _rule = new BlocklistRule("DefaultBlocklistRule");
    }
}