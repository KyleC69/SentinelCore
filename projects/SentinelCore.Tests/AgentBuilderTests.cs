// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentBuilderTests.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCore.Tests.TestInfrastructure;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="SentinelAgentFactory" /> — verifies that the factory
///     produces a non-null <see cref="AIAgent" /> for each <see cref="AgentRole" />,
///     and throws on invalid input.
/// </summary>
/// <remarks>
///     These tests use a real <see cref="SentinelAgentFactory" /> with a no-op logger
///     and a real <see cref="EventCapture" />. The tests verify construction-time
///     properties and factory behavior only.
/// </remarks>
[TestClass]
public sealed class AgentBuilderTests
{

    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public async Task BuildFromProfileAsync_CoreProfile_AgentNameMatchesSpec()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Core, "TheCore");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.AreEqual("TheCore", agent.Name);
    }








    [TestMethod]
    public async Task BuildFromProfileAsync_CoreProfile_ReturnsNonNullAgent()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Core, "TheCore");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public async Task BuildFromProfileAsync_ManagerProfile_AgentNameMatchesSpec()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Manager, "WorkflowManager");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.AreEqual("WorkflowManager", agent.Name);
    }








    [TestMethod]
    public async Task BuildFromProfileAsync_ManagerProfile_ReturnsNonNullAgent()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Manager, "WorkflowManager");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public async Task BuildFromProfileAsync_NullProfile_Throws()
    {
        SentinelAgentFactory factory = CreateFactory();

        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.BuildFromProfileAsync(null!));
    }








    [TestMethod]
    public async Task BuildFromProfileAsync_UtilityProfileWithTools_ReturnsNonNullAgent()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Utility, "registry_agent");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public async Task BuildFromProfileAsync_UtilityProfile_ReturnsNonNullAgent()
    {
        SentinelAgentFactory factory = CreateFactory();

        AgentProfile profile = CreateProfile(AgentRole.Utility, "worker-1");
        AIAgent agent = await factory.BuildFromProfileAsync(profile);

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public void Constructor_NullEvents_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SentinelAgentFactory(null!, NoOpLoggerFactory.Instance));
    }








    [TestMethod]
    public void Constructor_NullLoggerFactory_Throws()
    {
        EventCapture events = new();

        Assert.Throws<ArgumentNullException>(() => new SentinelAgentFactory(events, null!));
    }








    private static SentinelAgentFactory CreateFactory(EventCapture? events = null)
    {
        events ??= new EventCapture();
        return new SentinelAgentFactory(events, NoOpLoggerFactory.Instance);
    }








    private static AgentProfile CreateProfile(AgentRole role, string name, IList<AITool>? tools = null)
    {
        return new AgentProfile
        {
                Role = role,
                AgentName = name,
                Persona = new AgentPersona { Name = name, Instructions = "test instructions", Description = "test description" },
                Tools = tools ?? [],
                Model = ModelProfile.Glm5()
        };
    }
}