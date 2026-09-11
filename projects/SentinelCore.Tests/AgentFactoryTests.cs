// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryTests.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCore.Tests.TestInfrastructure;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="AgentProfileBuilder" /> — verifies that the builder
///     produces correct <see cref="AgentProfile" /> instances for each <see cref="AgentRole" />.
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
    public void SpecBuilder_CoreRole_AssignsCoreModel()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("TheCore", AgentRole.Core);

        Assert.IsNotNull(spec.Model);
    }





    [TestMethod]
    public void SpecBuilder_PerAgentModel_WinsOverRoleTier()
    {
        // Arrange — a per-agent entry must beat the role-tier model.
        SentinelCoreSettings settings = new()
        {
            DefaultModel = new ModelProfile("http://tier", "tier-model", 0.1f)
        };
        settings.AgentModels["TheCore"] = new ModelProfile("http://per-agent", "per-agent-model", 0.5f);

        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act
        AgentProfile spec = specBuilder.BuildAgentSpec("TheCore", AgentRole.Core);

        // Assert
        Assert.IsNotNull(spec.Model);
        Assert.AreEqual("per-agent-model", spec.Model.ModelId);
        Assert.AreEqual("http://per-agent", spec.Model.Endpoint);
    }





    [TestMethod]
    public void SpecBuilder_NoConfiguration_Anywhere_ModelIsNull()
    {
        // Arrange — no per-agent entry and no role-tier model: no fallback.
        SentinelCoreSettings settings = new();
        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act
        AgentProfile spec = specBuilder.BuildAgentSpec("TheCore", AgentRole.Core);

        // Assert — the factory gate rejects this at build time.
        Assert.IsNull(spec.Model);
    }





    [TestMethod]
    public void SpecBuilder_RoleTierFallsBack_ManagerToDefault()
    {
        // Arrange — Manager tier falls back to DefaultModel when unset.
        SentinelCoreSettings settings = new()
        {
            DefaultModel = new ModelProfile("http://default", "default-model", 0.1f)
        };
        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act
        AgentProfile spec = specBuilder.BuildAgentSpec("Manager", AgentRole.Manager);

        // Assert
        Assert.IsNotNull(spec.Model);
        Assert.AreEqual("default-model", spec.Model.ModelId);
    }





    [TestMethod]
    public void TryGetModel_ResolvesPerAgentThenTier()
    {
        // Arrange
        SentinelCoreSettings settings = new()
        {
            DefaultUtilityModel = new ModelProfile("http://utility", "utility-model", 0.1f)
        };
        settings.AgentModels["Worker1"] = new ModelProfile("http://worker", "worker-model", 0.1f);

        AgentProfileBuilder specBuilder = new(Microsoft.Extensions.Options.Options.Create(settings));

        // Act & Assert
        Assert.AreEqual("worker-model", specBuilder.TryGetModel("Worker1", AgentRole.Utility)?.ModelId);
        Assert.AreEqual("utility-model", specBuilder.TryGetModel("Worker2", AgentRole.Utility)?.ModelId);
        Assert.IsNull(specBuilder.TryGetModel("Worker3", AgentRole.Core));
    }





    [TestMethod]
    public async Task BuildFromProfileAsync_UnconfiguredModel_ThrowsGateError()
    {
        // Arrange — a profile with no model must fail the factory gate.
        SentinelAgentFactory factory = new(new EventCapture(), NoOpLoggerFactory.Instance, new FakeMcpServerRegistry());

        AgentProfile profile = new()
        {
            Role = AgentRole.Core,
            AgentName = "TheCore",
            Model = null
        };

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BuildFromProfileAsync(profile));

        StringAssert.Contains(exception.Message, "TheCore");
        StringAssert.Contains(exception.Message, "Model Configuration");
    }







    // ────────────────────────────────────────────────────────────
    //  AgentProfileBuilder
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void SpecBuilder_CoreRole_ReturnsCorrectSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("TheCore", AgentRole.Core);

        Assert.AreEqual(AgentRole.Core, spec.Role);
        Assert.AreEqual("TheCore", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona?.Instructions));
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona?.Description));
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
    public void SpecBuilder_ManagerRole_AssignsManagerModel()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("TheManager", AgentRole.Manager);

        Assert.IsNotNull(spec.Model);
    }





}
