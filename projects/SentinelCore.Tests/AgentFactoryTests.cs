// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryTests.cs
// Author: Kyle L. Crowder
// Build Num:  081602



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








    // ────────────────────────────────────────────────────────────
    //  AgentProfileBuilder
    // ────────────────────────────────────────────────────────────








    [TestMethod]
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








    [TestMethod]
    public void SpecBuilder_ManagerRole_ReturnsCorrectSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("TheManager", AgentRole.Manager);

        Assert.AreEqual(AgentRole.Manager, spec.Role);
        Assert.AreEqual("TheManager", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona?.Instructions));
    }








    [TestMethod]
    public void SpecBuilder_UnsupportedRole_Throws()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());
        Assert.Throws<ArgumentOutOfRangeException>(() => specBuilder.BuildAgentSpec("BadAgent", (AgentRole)999));
    }








    [TestMethod]
    public void SpecBuilder_UtilityRole_AssignsUtilityModel()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("worker-1", AgentRole.Utility);

        Assert.IsNotNull(spec.Model);
    }








    [TestMethod]
    public void SpecBuilder_WithCustomName_ReturnsNonNullSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("CustomAgent");

        Assert.IsNotNull(spec);
        Assert.AreEqual("CustomAgent", spec.AgentName);
    }








    [TestMethod]
    public void SpecBuilder_WithTaskInstructions_SetsInstructions()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec("TestAgent", AgentRole.Core, "Custom task instructions");

        Assert.AreEqual("Custom task instructions", spec.Instructions);
    }
}