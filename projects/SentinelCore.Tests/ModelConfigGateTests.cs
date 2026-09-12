// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ModelConfigGateTests.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="ModelConfigGate" /> covering completeness
///     evaluation, unconfigured-agent reporting, and gate message content.
/// </summary>
[TestClass]
public sealed class ModelConfigGateTests
{

    [TestMethod]
    public void AgentNamesAreCaseInsensitive()
    {
        // Arrange — the settings map is OrdinalIgnoreCase; "thecore" configures "TheCore".
        SentinelCoreSettings settings = new();
        settings.AgentModels["thecore"] = new ModelProfile("http://localhost:11434", "model-a", 0.1f);

        ModelConfigGate gate = CreateGate(["TheCore"], settings);

        // Act & Assert
        Assert.IsTrue(gate.IsConfigurationComplete);
    }








    [TestMethod]
    public void AllAgentsConfigured_IsComplete()
    {
        // Arrange
        SentinelCoreSettings settings = new();
        settings.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "model-a", 0.1f);
        settings.AgentModels["Manager"] = new ModelProfile("http://localhost:11434", "model-b", 0.1f);

        ModelConfigGate gate = CreateGate(["TheCore", "Manager"], settings);

        // Act & Assert
        Assert.IsTrue(gate.IsConfigurationComplete);
        Assert.AreEqual(0, gate.UnconfiguredAgents.Count);
        Assert.AreEqual(string.Empty, gate.BuildGateMessage());
    }








    [TestMethod]
    public void Constructor_NullBuilder_Throws()
    {
        // Arrange
        Mock<ISentinelAgentCatalog> catalog = new();
        SentinelCoreSettings settings = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigGate(catalog.Object, null!, settings));
    }








    [TestMethod]
    public void Constructor_NullCatalog_Throws()
    {
        // Arrange
        SentinelCoreSettings settings = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigGate(null!, new AgentProfileBuilder(Microsoft.Extensions.Options.Options.Create(settings)), settings));
    }








    [TestMethod]
    public void Constructor_NullSettings_Throws()
    {
        // Arrange
        Mock<ISentinelAgentCatalog> catalog = new();
        AgentProfileBuilder builder = new(Microsoft.Extensions.Options.Options.Create(new SentinelCoreSettings()));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigGate(catalog.Object, builder, null!));
    }








    /// <summary>
    ///     Creates a gate wired to a catalog advertising the given agent names
    ///     and a profile builder resolving models from the supplied settings.
    /// </summary>
    /// <param name="agentNames">The catalog agent names.</param>
    /// <param name="settings">The settings the builder resolves against.</param>
    /// <returns>A gate over the catalog and settings.</returns>
    private static ModelConfigGate CreateGate(string[] agentNames, SentinelCoreSettings settings)
    {
        Mock<ISentinelAgentCatalog> catalog = new();
        catalog.Setup(c => c.GetAgentNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(agentNames);

        AgentProfileBuilder builder = new(Microsoft.Extensions.Options.Options.Create(settings));

        return new ModelConfigGate(catalog.Object, builder, settings);
    }








    [TestMethod]
    public void MissingAgent_IsIncompleteAndReported()
    {
        // Arrange
        SentinelCoreSettings settings = new();
        settings.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "model-a", 0.1f);

        ModelConfigGate gate = CreateGate(["TheCore", "Manager", "Worker1"], settings);

        // Act & Assert
        Assert.IsFalse(gate.IsConfigurationComplete);
        CollectionAssert.AreEquivalent(new[] { "Manager", "Worker1" }, (System.Collections.ICollection)gate.UnconfiguredAgents);

        string message = gate.BuildGateMessage();
        StringAssert.Contains(message, "Manager");
        StringAssert.Contains(message, "Worker1");
        StringAssert.Contains(message, "Model Configuration");
    }








    [TestMethod]
    public void NoAgentsConfigured_AllReported()
    {
        // Arrange
        SentinelCoreSettings settings = new();
        ModelConfigGate gate = CreateGate(["TheCore", "Manager"], settings);

        // Act & Assert
        Assert.IsFalse(gate.IsConfigurationComplete);
        Assert.AreEqual(2, gate.UnconfiguredAgents.Count);
    }
}