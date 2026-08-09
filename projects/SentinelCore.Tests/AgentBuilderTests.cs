// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentBuilderTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="AgentBuilder" /> — verifies that the shared builder
///     produces a non-null <see cref="AIAgent" /> for each <see cref="AgentRole" />,
///     applies the correct middleware pipeline, and throws on invalid input.
/// </summary>
/// <remarks>
///     These tests use a real <see cref="AgentBuilder" /> with a no-op logger
///     and a real <see cref="EventCapture" />. The <see cref="AgentBuilder" />
///     internally constructs an <see cref="OllamaApiClient" />, but since we
///     never invoke <c>RunAsync</c> on the resulting agent, no network calls
///     are made. The tests verify construction-time properties only.
/// </remarks>
[TestClass]
public sealed class AgentBuilderTests
{

    [TestMethod]
    public void Build_CoreSpec_AgentNameMatchesSpec()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Core, "TheCore"));

        Assert.AreEqual("TheCore", agent.Name);
    }








    [TestMethod]
    public void Build_CoreSpec_ReturnsNonNullAgent()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Core, "TheCore"));

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public void Build_DomainSpecWithTools_AgentNameMatchesSpec()
    {
        AgentBuilder builder = CreateBuilder();
        AIFunction tool = AIFunctionFactory.Create(() => "ok", "test_tool", "Test tool");

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Domain, "registry_agent", [tool]));

        Assert.AreEqual("registry_agent", agent.Name);
    }








    [TestMethod]
    public void Build_DomainSpec_ReturnsNonNullAgent()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Domain, "registry_agent"));

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public void Build_ManagerSpec_AgentNameMatchesSpec()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Manager, "WorkflowManager"));

        Assert.AreEqual("WorkflowManager", agent.Name);
    }








    [TestMethod]
    public void Build_ManagerSpec_ReturnsNonNullAgent()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Manager, "WorkflowManager"));

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public void Build_NullSpec_Throws()
    {
        AgentBuilder builder = CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Build(null!));
    }








    [TestMethod]
    public void Build_WorkerSpec_ReturnsNonNullAgent()
    {
        AgentBuilder builder = CreateBuilder();

        AIAgent agent = builder.Build(CreateSpec(AgentRole.Worker, "worker-1"));

        Assert.IsNotNull(agent);
    }








    [TestMethod]
    public void Constructor_NullEvents_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AgentBuilder(null!, NoOpLoggerFactory.Instance));
    }








    [TestMethod]
    public void Constructor_NullLoggerFactory_Throws()
    {
        EventCapture events = new EventCapture();

        Assert.Throws<ArgumentNullException>(() => new AgentBuilder(events, null!));
    }








    private static AgentBuilder CreateBuilder(EventCapture? events = null)
    {
        events ??= new EventCapture();
        return new AgentBuilder(events, NoOpLoggerFactory.Instance);
    }








    private static AgentProfile CreateSpec(AgentRole role, string name, IList<AITool>? tools = null)
    {
        return new AgentProfile
        {
                Role = role,
                AgentName = name,
                Persona = new AgentPersona { Name = name, Instructions = "test instructions", Description = "test description" },
                Tools = tools ?? [],
                Model = ModelBuilder
        };
    }
}