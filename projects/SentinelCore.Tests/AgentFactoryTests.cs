// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for agent factories — verifies that each factory delegates
///     to <see cref="IAgentBuilder" /> with the correct <see cref="AgentProfile" />
///     (role, name, persona, tools, model settings). These tests prevent
///     architectural drift in the factory layer.
///     <para>
///         After the pipeline streamlining, factories delegate to
///         <see cref="IAgentSpecBuilder" /> for base spec assembly (persona,
///         model, role-based defaults). Tests verify that the factory correctly
///         overrides or augments the base spec where needed.
///     </para>
/// </summary>
[TestClass]
public sealed class AgentFactoryTests
{

    // ────────────────────────────────────────────────────────────
    //  CoreAgentFactory
    // ────────────────────────────────────────────────────────────








    /// <summary>
    ///     Verifies that CoreAgentFactory produces a spec with the Core role,
    ///     the correct agent name, a non-empty persona, and MCP research tools.
    /// </summary>
    [TestMethod]
    public void CoreFactory_DelegatesToAgentBuilder_WithCoreSpec()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        CoreAgentFactory factory = new(specBuilder, builder);

        factory.Create();

        AgentProfile spec = builder.AssertSingleSpec();
        Assert.AreEqual(AgentRole.Core, spec.Role);
        Assert.AreEqual("TheCore", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Instructions));
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Description));
        Assert.IsTrue(spec.Tools.Count > 0, "Core agent should have research tools.");
        Assert.AreEqual("test-core", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that CoreAgentFactory throws when the agent builder is null.
    /// </summary>
    [TestMethod]
    public void CoreFactory_NullBuilder_Throws()
    {
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        Assert.Throws<ArgumentNullException>(() => new CoreAgentFactory(specBuilder, null!));
    }








    /// <summary>
    ///     Verifies that CoreAgentFactory throws when the spec builder is null.
    /// </summary>
    [TestMethod]
    public void CoreFactory_NullSpecBuilder_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        Assert.Throws<ArgumentNullException>(() => new CoreAgentFactory(null!, builder));
    }








    /// <summary>
    ///     Verifies that the Core agent's persona instructions contain the
    ///     canonical domain list used for investigation planning.
    /// </summary>
    [TestMethod]
    public void CoreFactory_PersonaInstructionsContainDomainList()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        CoreAgentFactory factory = new(specBuilder, builder);

        factory.Create();

        AgentProfile spec = builder.LastSpec;
        Assert.IsTrue(spec.Persona.Instructions.Contains("registry"), "Instructions should list 'registry' domain.");
        Assert.IsTrue(spec.Persona.Instructions.Contains("filesystem"), "Instructions should list 'filesystem' domain.");
        Assert.IsTrue(spec.Persona.Instructions.Contains("defender"), "Instructions should list 'defender' domain.");
    }








    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────








    private static IDomainAgentFactory CreateDomainFactoryStub(CapturingAgentBuilder builder)
    {
        return new StubDomainAgentFactory(builder);
    }








    // ────────────────────────────────────────────────────────────
    //  DomainAgentFactory
    // ────────────────────────────────────────────────────────────








    /// <summary>
    ///     Verifies that DomainAgentFactory produces a spec with the Domain role,
    ///     the correct agent name, domain-specific tools, and the provided description
    ///     overriding the persona's default description.
    /// </summary>
    [TestMethod]
    public void DomainFactory_DelegatesToAgentBuilder_WithDomainSpec()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        DomainAgentFactory factory = new(specBuilder, builder);

        factory.CreateAgent("registry", "Read registry keys");

        AgentProfile spec = builder.AssertSingleSpec();
        Assert.AreEqual(AgentRole.Domain, spec.Role);
        Assert.AreEqual("registry_agent", spec.AgentName);
        Assert.AreEqual("Read registry keys", spec.Persona.Description);
        Assert.IsTrue(spec.Tools.Count > 0, "Domain agent should have tools from registry.");
        Assert.AreEqual("test-domain", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that when a blank description is provided, the persona's
    ///     default description is preserved (not overridden with whitespace).
    /// </summary>
    [TestMethod]
    public void DomainFactory_EmptyDescription_PreservesDefaultPersonaDescription()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        DomainAgentFactory factory = new(specBuilder, builder);

        factory.CreateAgent("wmi", "");

        AgentProfile spec = builder.LastSpec;
        // When description is whitespace, the factory should keep the default persona description.
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Description));
    }








    /// <summary>
    ///     Verifies that DomainAgentFactory throws when domain is empty.
    /// </summary>
    [TestMethod]
    public void DomainFactory_EmptyDomain_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        DomainAgentFactory factory = new(specBuilder, builder);

        Assert.Throws<ArgumentException>(() => factory.CreateAgent("", "test"));
    }








    /// <summary>
    ///     Verifies that DomainAgentFactory throws when the agent builder is null.
    /// </summary>
    [TestMethod]
    public void DomainFactory_NullBuilder_Throws()
    {
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        Assert.Throws<ArgumentNullException>(() => new DomainAgentFactory(specBuilder, null!));
    }








    /// <summary>
    ///     Verifies that DomainAgentFactory throws when domain is null.
    /// </summary>
    [TestMethod]
    public void DomainFactory_NullDomain_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        DomainAgentFactory factory = new(specBuilder, builder);

        Assert.Throws<ArgumentException>(() => factory.CreateAgent(null!, "test"));
    }








    /// <summary>
    ///     Verifies that DomainAgentFactory throws when the spec builder is null.
    /// </summary>
    [TestMethod]
    public void DomainFactory_NullSpecBuilder_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        Assert.Throws<ArgumentNullException>(() => new DomainAgentFactory(null!, builder));
    }








    /// <summary>
    ///     Verifies that an unknown domain produces an agent with no tools.
    /// </summary>
    [TestMethod]
    public void DomainFactory_UnknownDomain_ProducesEmptyTools()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        DomainAgentFactory factory = new(specBuilder, builder);

        factory.CreateAgent("nonexistent", "test");

        AgentProfile spec = builder.LastSpec;
        Assert.AreEqual(0, spec.Tools.Count, "Unknown domain should produce empty tools list.");
    }








    // ────────────────────────────────────────────────────────────
    //  ManagerAgentFactory
    // ────────────────────────────────────────────────────────────








    /// <summary>
    ///     Verifies that ManagerAgentFactory produces a spec with the Manager role,
    ///     the correct agent name, no tools, and a non-empty persona.
    /// </summary>
    [TestMethod]
    public void ManagerFactory_DelegatesToAgentBuilder_WithManagerSpec()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        ManagerAgentFactory factory = new(CreateDomainFactoryStub(builder), specBuilder, builder);

        factory.Create();

        AgentProfile spec = builder.CapturedSpecs.LastOrDefault() ?? throw new InvalidOperationException("No spec was captured.");
        Assert.AreEqual(AgentRole.Manager, spec.Role);
        Assert.AreEqual("TheManager", spec.AgentName);
        Assert.AreEqual(0, spec.Tools.Count, "Manager agent should have no tools.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Instructions));
        Assert.AreEqual("test-manager", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that ManagerAgentFactory throws when the domain factory is null.
    /// </summary>
    [TestMethod]
    public void ManagerFactory_NullDomainFactory_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IAgentSpecBuilder specBuilder = new AgentSpecBuilder(TestOptions.Create());
        Assert.Throws<ArgumentNullException>(() => new ManagerAgentFactory(null!, specBuilder, builder));
    }








    /// <summary>
    ///     Verifies that ManagerAgentFactory throws when the spec builder is null.
    /// </summary>
    [TestMethod]
    public void ManagerFactory_NullSpecBuilder_Throws()
    {
        CapturingAgentBuilder builder = new CapturingAgentBuilder();
        IDomainAgentFactory domainFactory = CreateDomainFactoryStub(builder);
        Assert.Throws<ArgumentNullException>(() => new ManagerAgentFactory(domainFactory, null!, builder));
    }








    // ────────────────────────────────────────────────────────────
    //  AgentSpecBuilder
    // ────────────────────────────────────────────────────────────








    /// <summary>
    ///     Verifies that AgentSpecBuilder produces a spec with the correct role,
    ///     agent name, persona, and model for the Core role.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_CoreRole_ReturnsCorrectSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec(AgentRole.Core);

        Assert.AreEqual(AgentRole.Core, spec.Role);
        Assert.AreEqual("TheCore", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Instructions));
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Description));
        Assert.AreEqual("test-core", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that AgentSpecBuilder produces a spec with the correct role,
    ///     agent name, and model for the Domain role.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_DomainRole_ReturnsCorrectSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec(AgentRole.Domain);

        Assert.AreEqual(AgentRole.Domain, spec.Role);
        Assert.AreEqual("TheDomainAgent", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Instructions));
        Assert.AreEqual("test-domain", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that AgentSpecBuilder produces a spec with the correct role,
    ///     agent name, and model for the Manager role.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_ManagerRole_ReturnsCorrectSpec()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());

        AgentProfile spec = specBuilder.BuildAgentSpec(AgentRole.Manager);

        Assert.AreEqual(AgentRole.Manager, spec.Role);
        Assert.AreEqual("TheManager", spec.AgentName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(spec.Persona.Instructions));
        Assert.AreEqual("test-manager", spec.Model.ModelId);
    }








    /// <summary>
    ///     Verifies that AgentSpecBuilder throws when a null persona is provided
    ///     to the override method.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_NullPersona_Throws()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());
        Assert.Throws<ArgumentNullException>(() => specBuilder.BuildAgentSpec(AgentRole.Core, null!));
    }








    /// <summary>
    ///     Verifies that AgentSpecBuilder uses the provided persona override
    ///     instead of the role-based default.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_PersonaOverride_ReplacesDefaultPersona()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());
        AgentPersona customPersona = new() { Name = "CustomAgent", Description = "Custom description", Instructions = "Custom instructions" };

        AgentProfile spec = specBuilder.BuildAgentSpec(AgentRole.Core, customPersona);

        Assert.AreEqual("CustomAgent", spec.Persona.Name);
        Assert.AreEqual("Custom description", spec.Persona.Description);
        Assert.AreEqual("Custom instructions", spec.Persona.Instructions);
    }








    /// <summary>
    ///     Verifies that AgentSpecBuilder throws when an unsupported role is provided.
    /// </summary>
    [TestMethod]
    public void SpecBuilder_UnsupportedRole_Throws()
    {
        AgentProfileBuilder specBuilder = new(TestOptions.Create());
        Assert.Throws<ArgumentOutOfRangeException>(() => specBuilder.BuildAgentSpec((AgentRole)999));
    }








    // ────────────────────────────────────────────────────────────
    //  Stub factories for testing
    // ────────────────────────────────────────────────────────────





    private sealed class StubDomainAgentFactory : IDomainAgentFactory
    {
        private readonly CapturingAgentBuilder _builder;








        public StubDomainAgentFactory(CapturingAgentBuilder builder)
        {
            _builder = builder;
        }








        public Microsoft.Agents.AI.AIAgent CreateAgent(string domain, string description)
        {
            return _builder.Build(new AgentProfile
            {
                    Role = AgentRole.Domain,
                    AgentName = $"{domain}_agent",
                    Persona = new AgentPersona { Name = $"{domain}_agent", Instructions = "", Description = description },
                    Tools = [],
                    Model = new ModelSettings("http://localhost:11434", "test-model")
            });
        }
    }
}