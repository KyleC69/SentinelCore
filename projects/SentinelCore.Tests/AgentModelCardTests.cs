// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentModelCardTests.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Contracts.Contracts;
using SentinelCore.UI.Models;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="AgentModelCard" /> covering construction,
///     profile population, and the completeness rules that drive the gate.
/// </summary>
[TestClass]
public sealed class AgentModelCardTests
{
    [TestMethod]
    public void Constructor_EmptyAgentName_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AgentModelCard(""));
    }








    [TestMethod]
    public void Constructor_NullProfile_StartsUnconfigured()
    {
        // Arrange & Act
        AgentModelCard card = new("TheCore");

        // Assert
        Assert.AreEqual("TheCore", card.AgentName);
        Assert.IsFalse(card.IsConfigured);
        Assert.IsFalse(card.IsComplete);
        Assert.IsNull(card.ToModelProfile());
    }











    [TestMethod]
    public void ToModelProfile_CompleteCard_BuildsProfile()
    {
        // Arrange
        AgentModelCard card = new("TheCore")
        {
            Endpoint = "http://localhost:11434",
            ModelId = "model-a",
            Temperature = 0.3f,
            MaxOutputTokens = 12000,
            TopK = 1,
            TopP = 0.2f
        };

        // Act
        ModelProfile? profile = card.ToModelProfile();

        // Assert
        Assert.IsNotNull(profile);
        Assert.AreEqual("http://localhost:11434", profile.Endpoint);
        Assert.AreEqual("model-a", profile.ModelId);
        Assert.AreEqual(0.3f, profile.Temperature);
        Assert.AreEqual(12000, profile.MaxOutputTokens);
    }








    [TestMethod]
    public void ToModelProfile_MissingEndpoint_ReturnsNull()
    {
        // Arrange — model set but no endpoint.
        AgentModelCard card = new("TheCore") { ModelId = "model-a" };

        // Act & Assert
        Assert.IsFalse(card.IsComplete);
        Assert.IsNull(card.ToModelProfile());
    }








    [TestMethod]
    public void ToModelProfile_MissingModelId_ReturnsNull()
    {
        // Arrange — endpoint set but no model.
        AgentModelCard card = new("TheCore") { Endpoint = "http://localhost:11434" };

        // Act & Assert
        Assert.IsFalse(card.IsComplete);
        Assert.IsNull(card.ToModelProfile());
    }








    [TestMethod]
    public void ToModelProfile_TrimsWhitespace()
    {
        // Arrange
        AgentModelCard card = new("TheCore") { Endpoint = "  http://localhost:11434  ", ModelId = "  model-a  " };

        // Act
        ModelProfile? profile = card.ToModelProfile();

        // Assert
        Assert.IsNotNull(profile);
        Assert.AreEqual("http://localhost:11434", profile.Endpoint);
        Assert.AreEqual("model-a", profile.ModelId);
    }
}