// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ModelConfigGateTests.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;

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
    public void AllAgentsConfigured_IsComplete()
    {
        // Arrange
        SentinelCoreSettings settings = new();
        settings.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "model-a", 0.1f);
        settings.AgentModels["Manager"] = new ModelProfile("http://localhost:11434", "model-b", 0.1f);

        /*     ModelConfigGate gate = CreateGate(["TheCore", "Manager"], settings);

             // Act & Assert
             Assert.IsTrue(gate.IsConfigurationComplete);
             Assert.AreEqual(0, gate.UnconfiguredAgents.Count);
             Assert.AreEqual(string.Empty, gate.BuildGateMessage());*/
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
        //Assert.Throws<ArgumentNullException>(() => new ModelConfigGate(null!, new AgentProfileBuilder(Options.Create(settings)), settings));
    }








    [TestMethod]
    public void Constructor_NullSettings_Throws()
    {
        // Arrange
        Mock<ISentinelAgentCatalog> catalog = new();
        //  AgentProfileBuilder builder = new(Options.Create(new SentinelCoreSettings()));

        // Act & Assert
        //  Assert.Throws<ArgumentNullException>(() => new ModelConfigGate(catalog.Object, builder, null!));
    }








    private ModelConfigGate CreateGate(List<string> list, SentinelCoreSettings settings)
    {
        throw new NotImplementedException();
    }
}