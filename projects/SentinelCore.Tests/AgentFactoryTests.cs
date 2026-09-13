// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryTests.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Tests.TestInfrastructure;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="AgentProfileBuilder" />.
/// </summary>
[TestClass]
public sealed class AgentFactoryTests
{



    [TestMethod]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AgentProfileBuilder(null!));
    }








    [TestMethod]
    public void SpecBuilder_DefaultBuild_ReturnsNonNullSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec();

        Assert.IsNotNull(spec);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.AgentName));
    }








    [TestMethod]
    public void SpecBuilder_PerAgentModel_ReturnsCorrectModel()
    {
        // Arrange — a per-agent entry.
        SentinelCoreSettings settings = new() { DefaultModel = new ModelProfile("http://tier", "tier-model", 0.1f), AgentModels = { ["TheCore"] = new ModelProfile("http://per-agent", "per-agent-model", 0.5f) } };

        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act
        AgentProfile spec = specBuilder.BuildAgentSpec("TheCore");

        // Assert
        Assert.IsNotNull(spec.Model);
        Assert.AreEqual("per-agent-model", spec.Model.ModelId);
        Assert.AreEqual("http://per-agent", spec.Model.Endpoint);
    }








    [TestMethod]
    public void TryGetModel_ResolvesPerAgentModel()
    {
        // Arrange
        SentinelCoreSettings settings = new();
        settings.AgentModels["Worker1"] = new ModelProfile("http://worker", "worker-model", 0.1f);

        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act & Assert
        Assert.AreEqual("worker-model", specBuilder.TryGetModel("Worker1")?.ModelId);
        Assert.IsNull(specBuilder.TryGetModel("Worker3"));
    }
}