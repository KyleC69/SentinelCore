// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         AgentFactoryToolTests.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for the <see cref="SentinelAgentFactory" /> focusing on tool availability.
/// </summary>
[TestClass]
public sealed class AgentFactoryToolTests
{

    private FakeChatClient _chatClient = null!;
    private Mock<IChatClientFactory> _chatClientFactoryMock = null!;
    private SentinelAgentFactory _factory = null!;
    private Mock<ILoggerFactory> _loggerFactoryMock = null!;
    private Mock<IMcpServerRegistry> _mcpServerRegistryMock = null!;
    private Mock<IModelResolver> _modelResolverMock = null!;
    private Mock<IPatternMatcher> _patternMatcherMock = null!;
    private const string PromptText = "ping";
    private const string RegistryToolName = "registry_noop";
    private const string LocalToolName = "local_noop";








    /// <summary>
    ///     Verifies that the tools discovered by the MCP registry reach the model call issued by a Worker
    ///     agent instead of being dropped while the agent is built.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateWorkerAgent_AttachesRegistryToolsToModelCallAsync()
    {
        // Arrange
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.WorkerAgent);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions? options = _chatClient.LastOptions;
        Assert.IsNotNull(options, "Running the agent must issue a chat request to the model provider.");
        Assert.IsNotNull(options.Tools, "The chat request must carry the agent's tool definitions.");
        Assert.IsTrue(options.Tools.Any(tool => tool.Name == RegistryToolName), $"The registry tool '{RegistryToolName}' never reached the model call, so the agent could not invoke it.");
    }








    /// <summary>
    ///     Configures a factory whose external collaborators are deterministic test doubles: a chat client
    ///     that records what it was asked to send, and a registry that exposes exactly one tool.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _chatClient = new(new(new ChatMessage(ChatRole.Assistant, "pong")));

        _chatClientFactoryMock = new();
        _chatClientFactoryMock.Setup(factory => factory.CreateChatClient(It.IsAny<ModelProfile>())).Returns(_chatClient);

        _modelResolverMock = new();
        _modelResolverMock.Setup(resolver => resolver.ResolveByTier(It.IsAny<ModelTier>())).Returns(new ModelProfile("test-model", "http://test.endpoint", 0.7f));

        _patternMatcherMock = new();

        _loggerFactoryMock = new();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        _mcpServerRegistryMock = new();
        _mcpServerRegistryMock.Setup(x => x.ToolCount).Returns(1);

        _factory = CreateFactory([AIFunctionFactory.Create(() => "ok", RegistryToolName)]);
    }





    /// <summary>
    ///     Verifies that omitting a tool selection binds the preset's role toolset: the Worker role claims the
    ///     registry tool and leaves the unrelated local tool unbound.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithoutToolSelection_BindsTheRoleToolsetAsync()
    {
        // Arrange
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.WorkerAgent);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.IsTrue(options.Tools!.Any(tool => tool.Name == RegistryToolName), "The Worker role toolset must bind the registry tool.");
        Assert.IsFalse(options.Tools!.Any(tool => tool.Name == LocalToolName), "The Worker role toolset must not bind tools outside its domains.");
    }





    /// <summary>
    ///     Verifies that an explicit <see cref="AgentToolSelection.None" /> selection removes every tool
    ///     from an agent whose preset would otherwise bind the whole MCP catalog.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithNoneSelection_BindsNoToolsAsync()
    {
        // Arrange
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.CoreAgent, AgentToolSelection.None);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.IsNotNull(options.Tools, "An empty toolset must still be reported as an empty collection, never null.");
        Assert.AreEqual(0, options.Tools!.Count, "The None selection must bind no tools at all.");
    }





    /// <summary>
    ///     Verifies that an <see cref="AgentToolSelection.All" /> selection binds the merged catalog so a
    ///     single agent can use both MCP-server tools and local in-process tools.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithAllSelection_BindsMcpAndLocalToolsAsync()
    {
        // Arrange
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.CoreAgent, AgentToolSelection.All);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.IsTrue(options.Tools!.Any(tool => tool.Name == RegistryToolName), "The All selection must include MCP-server tools.");
        Assert.IsTrue(options.Tools!.Any(tool => tool.Name == LocalToolName), "The All selection must include local in-process tools.");
    }





    /// <summary>
    ///     Verifies that a named selection binds exactly the requested tools, resolved case-insensitively,
    ///     and nothing else.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithNamedSelection_BindsOnlyRequestedToolsAsync()
    {
        // Arrange
        AgentToolSelection selection = AgentToolSelection.FromNames(LocalToolName.ToUpperInvariant());

        // Act
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.CoreAgent, selection);
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.AreEqual(1, options.Tools!.Count, "A single named tool must produce a single bound tool.");
        Assert.AreEqual(LocalToolName, options.Tools![0].Name, "The named selection must resolve the tool name case-insensitively.");
    }





    /// <summary>
    ///     Verifies that a misspelled tool name fails fast at construction instead of silently producing
    ///     an agent that cannot do its job.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithUnknownToolName_ThrowsAsync()
    {
        // Arrange
        AgentToolSelection selection = AgentToolSelection.FromNames("tool-that-does-not-exist");

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _factory.CreateAgentAsync(AgentPreset.CoreAgent, selection));
        Assert.Contains("tool-that-does-not-exist", exception.Message, "The failure must name the unresolved tool so the call site can be corrected.");
    }





    /// <summary>
    ///     Verifies that the architecture rule forbidding tools on the Manager agent is enforced
    ///     structurally by the factory rather than by call-site discipline.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WhenManagerRequestsTools_ThrowsAsync()
    {
        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _factory.CreateAgentAsync(AgentPreset.ManagerAgent, AgentToolSelection.All));
        Assert.Contains("Manager", exception.Message, "The failure must explain that the Manager agent may never hold a toolset.");
    }





    /// <summary>
    ///     Verifies that agent construction no longer depends on at least one MCP server being available,
    ///     which previously aborted the whole workflow when no server had started yet.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WhenMcpCatalogIsEmpty_StillCreatesAgentAsync()
    {
        // Arrange
        SentinelAgentFactory factory = CreateFactory([]);

        // Act
        AIAgent agent = await factory.CreateAgentAsync(AgentPreset.CoreAgent);

        // Assert
        Assert.IsNotNull(agent, "Agent construction must succeed even when no MCP server has published tools.");
    }





    /// <summary>
    ///     Verifies that the call-site agent identity reaches the constructed agent, so sub-workflow
    ///     agents do not silently share the preset's default name.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithCallSiteIdentity_AppliesNameAndDescriptionAsync()
    {
        // Arrange
        const string AgentName = "utility-alpha";
        const string AgentDescription = "Utility agent for the alpha sub-workflow.";

        // Act
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.UtilityAgent, AgentName, AgentDescription);

        // Assert
        Assert.AreEqual(AgentName, agent.Name, "The call-site agent name must win over the preset default.");
        Assert.AreEqual(AgentDescription, agent.Description, "The call-site agent description must win over the preset default.");
    }





    /// <summary>
    ///     Verifies that duplicate tool names are collapsed case-insensitively so a call site cannot bind
    ///     the same tool twice.
    /// </summary>
    [TestMethod]
    public void AgentToolSelectionFromNames_CollapsesDuplicateNamesCaseInsensitively()
    {
        // Arrange & Act
        AgentToolSelection selection = AgentToolSelection.FromNames("create_case", "CREATE_CASE", "insert_report");

        // Assert
        Assert.AreEqual(2, selection.ToolNames.Count, "Duplicate tool names must be collapsed.");
        Assert.AreEqual(AgentToolSelectionKind.Named, selection.Kind);
    }





    /// <summary>
    ///     Verifies that a domain selection binds only the tools its rules claim and leaves the rest of the
    ///     catalogue unbound.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithDomainSelection_BindsOnlyClaimedToolsAsync()
    {
        // Arrange
        AgentToolDomain domain = new("RegistryOnly", "Claims registry tools only.", ToolMatchRule.Prefix("registry"));

        // Act
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromDomains(domain));
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.AreEqual(1, options.Tools!.Count, "A domain that claims one tool must bind exactly one tool.");
        Assert.AreEqual(RegistryToolName, options.Tools![0].Name, "The domain must bind the tool its rule claims.");
    }





    /// <summary>
    ///     Verifies that overlapping domains bind a shared tool once, so a tool claimed by several domains
    ///     cannot be duplicated on the model call.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithOverlappingDomains_BindsSharedToolOnceAsync()
    {
        // Arrange
        AgentToolDomain first = new("First", "Claims registry tools.", ToolMatchRule.Prefix("registry"));
        AgentToolDomain second = new("Second", "Also claims registry tools.", ToolMatchRule.Prefix("REGISTRY"));

        // Act
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromDomains(first, second));
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.AreEqual(1, options.Tools!.Count, "A tool claimed by two domains must be bound exactly once.");
        Assert.AreEqual(RegistryToolName, options.Tools![0].Name);
    }





    /// <summary>
    ///     Verifies that a domain claiming no live tool is a soft condition: the agent is still built, with
    ///     an empty toolset, because a domain can legitimately be empty when its server is not loaded.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task CreateAgentAsync_WithEmptyDomain_StillCreatesAgentWithNoToolsAsync()
    {
        // Arrange
        AgentToolDomain absent = new("Absent", "Claims nothing in the catalogue.", ToolMatchRule.Prefix("not_a_real_area"));

        // Act
        AIAgent agent = await _factory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromDomains(absent));
        await agent.RunAsync(PromptText);

        // Assert
        ChatOptions options = GetRecordedOptions();
        Assert.AreEqual(0, options.Tools!.Count, "An empty domain must produce an empty toolset rather than a failure.");
    }





    /// <summary>
    ///     Verifies that the prefix matcher normalises the separator, so a prefix for one area never claims a
    ///     longer sibling area that merely shares its leading characters.
    /// </summary>
    [TestMethod]
    public void ToolMatchRulePrefix_DoesNotClaimLongerSiblingArea()
    {
        // Arrange
        ToolMatchRule rule = ToolMatchRule.Prefix("Power");

        // Act & Assert
        Assert.IsTrue(rule.Matches("Power_Read_Configuration"), "The prefix must claim its own area.");
        Assert.IsFalse(rule.Matches("PowerShell_Run_Command"), "The prefix must not claim the longer sibling area.");
    }





    /// <summary>
    ///     Verifies that the token matcher is vendor independent, so a tool keeps its domain when a server
    ///     drops or adds a vendor prefix.
    /// </summary>
    [TestMethod]
    public void ToolMatchRuleToken_MatchesRegardlessOfVendorPrefix()
    {
        // Arrange
        ToolMatchRule rule = ToolMatchRule.Token("PsList");

        // Act & Assert
        Assert.IsTrue(rule.Matches("PsList_List_Processes"), "The token must claim the bare name.");
        Assert.IsTrue(rule.Matches("Sysinternals_PsList_List_Processes"), "The token must claim the vendor-prefixed name.");
        Assert.IsFalse(rule.Matches("Process_List_Handles"), "The token must not claim an unrelated tool.");
    }





    /// <summary>
    ///     Verifies that the unclassified safety net reports exactly the tools no domain claims, so a newly
    ///     added tool surfaces as a warning instead of being silently unreachable.
    /// </summary>
    [TestMethod]
    public void AgentToolDomainCatalogUnclassified_ReportsOnlyUnclaimedTools()
    {
        // Arrange
        string[] catalog = [RegistryToolName, "local_case_create", "brand_new_area_do_something"];

        // Act
        IReadOnlyList<string> unclassified = AgentToolDomainCatalog.Unclassified(catalog);

        // Assert
        Assert.AreEqual(1, unclassified.Count, "Only the tool no rule claims may be reported.");
        Assert.AreEqual("brand_new_area_do_something", unclassified[0]);
    }





    /// <summary>
    ///     Verifies that every in-process tool the platform registers is claimed by a domain, so the local
    ///     tools cannot drift outside the vocabulary.
    /// </summary>
    [TestMethod]
    public void AgentToolDomainCatalog_ClaimsEveryLocalToolName()
    {
        // Arrange
        string[] localTools = ["local_case_create", "local_case_insert_report", "local_case_insert_pattern_summaries", "local_docs_query"];

        // Act
        IReadOnlyList<string> unclassified = AgentToolDomainCatalog.Unclassified(localTools);

        // Assert
        Assert.AreEqual(0, unclassified.Count, $"Every local tool must be claimed by a domain, but these were not: {string.Join(", ", unclassified)}");
    }





    /// <summary>
    ///     Verifies that role-based construction resolves to domains for a working role and to an empty
    ///     selection for the roles that are defined as tool-free.
    /// </summary>
    [TestMethod]
    public void AgentToolSelectionFromRole_ResolvesDomainsOrNoneByRole()
    {
        // Act
        AgentToolSelection core = AgentToolSelection.FromRole(AgentPreset.CoreAgent);
        AgentToolSelection manager = AgentToolSelection.FromRole(AgentPreset.ManagerAgent);

        // Assert
        Assert.AreEqual(AgentToolSelectionKind.Domains, core.Kind, "The Core role must resolve to its domain toolset.");
        Assert.IsTrue(core.Domains.Count > 0, "The Core role must carry at least one domain.");
        Assert.AreEqual(AgentToolSelectionKind.None, manager.Kind, "The Manager role must resolve to no tools, as PL-3 requires.");
    }





    /// <summary>
    ///     Verifies that a role toolset never contains an operating-system domain, keeping the Core agent
    ///     focused on case work, documentation and web research.
    /// </summary>
    [TestMethod]
    public void AgentToolRoleToolsets_ForCore_ExcludesOperatingSystemDomains()
    {
        // Act
        IReadOnlyList<AgentToolDomain> domains = AgentToolRoleToolsets.For(AgentPreset.CoreAgent);

        // Assert
        Assert.IsTrue(domains.Any(domain => domain.Name == AgentToolDomainCatalog.CaseLifecycle.Name), "The Core role must be able to run case work.");
        Assert.IsFalse(domains.Any(domain => domain.Name == AgentToolDomainCatalog.Registry.Name), "The Core role must not hold registry tools.");
        Assert.IsFalse(domains.Any(domain => domain.Name == AgentToolDomainCatalog.Process.Name), "The Core role must not hold process tools.");
    }





    /// <summary>
    ///     Verifies that duplicate domains are collapsed by name so a call site cannot request the same
    ///     domain twice.
    /// </summary>
    [TestMethod]
    public void AgentToolSelectionFromDomains_CollapsesDuplicateDomains()
    {
        // Arrange & Act
        AgentToolSelection selection = AgentToolSelection.FromDomains(AgentToolDomainCatalog.Registry, AgentToolDomainCatalog.Registry);

        // Assert
        Assert.AreEqual(1, selection.Domains.Count, "Duplicate domains must be collapsed.");
        Assert.AreEqual(AgentToolSelectionKind.Domains, selection.Kind);
    }





    /// <summary>
    ///     Builds a factory whose MCP registry publishes exactly the supplied tools and whose local tool
    ///     catalog contains one invocable in-process tool.
    /// </summary>
    /// <param name="mcpTools">The tools the fake MCP registry advertises.</param>
    /// <returns>The configured factory.</returns>
    private SentinelAgentFactory CreateFactory(IReadOnlyList<AITool> mcpTools)
    {
        _mcpServerRegistryMock.Setup(x => x.GetToolsFromCache()).Returns(mcpTools);

        return new(
            _chatClientFactoryMock.Object,
            _modelResolverMock.Object,
            _patternMatcherMock.Object,
            _loggerFactoryMock.Object,
            _mcpServerRegistryMock.Object,
            [AIFunctionFactory.Create(() => "local-ok", LocalToolName)],
            new()
            {
                ChatClientLoggingEnabled = true,
                OpenTelemetryTracingEnabled = true,
                OpenTelemetryLoggingEnabled = true,
                TraceEnabled = true
            });
    }





    /// <summary>
    ///     Runs the previously created agent and returns the chat options it sent to the model provider.
    /// </summary>
    /// <returns>The recorded chat options.</returns>
    private ChatOptions GetRecordedOptions()
    {
        ChatOptions? options = _chatClient.LastOptions;
        Assert.IsNotNull(options, "Running the agent must issue a chat request to the model provider.");

        return options;
    }
}
