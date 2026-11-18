// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Infrastructure.Diagnostics;
using SentinelCore.Orchestrations.Models;




// experimental API: suppressed with MAAI001




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Provides functionality to create and configure instances of <see cref="AIAgent" />
///     based on the provided <see cref="AgentProfile" /> or preset name.
///     <para>
///         This factory is the canonical composition boundary for the chat client,
///         context providers, tools, and agent options. It applies optional chat-client
///         logging and OpenTelemetry instrumentation according to <see cref="SentinelCoreSettings" />
///         and configures pattern memory and MAF context compaction from the agent preset.
///     </para>
///     <para>
///         Instructions and response format are per-call concerns handled by executors
///         at invocation time via <see cref="ChatMessages" /> and <see cref="AgentRunOptions" />.
///         The factory does NOT set <see cref="ChatOptions.Instructions" /> or
///         <see cref="ChatOptions.ResponseFormat" />.
///     </para>
/// </summary>
public sealed class SentinelAgentFactory : ISentinelAgentFactory
{
    private readonly Dictionary<string, string> _builtAgents = new(StringComparer.Ordinal);
    private readonly Lock _builtAgentsLock = new();
    private IReadOnlyList<AITool> _builtToolCache;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly bool _enableChatClientLogging;
    private readonly bool _enableOpenTelemetryTracing;
    private readonly IReadOnlyList<AITool> _localTools;
    private readonly ILogger<SentinelAgentFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    // The legacy profile builder is no longer used for new code.
    // private readonly IAgentProfileBuilder _profileBuilder;
    private readonly IModelResolver _modelResolver;

    private readonly IPatternMatcher _patternMatcher;
    private readonly IMcpServerRegistry _registry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="modelResolver"></param>
    /// <param name="patternMatcher">The pattern matcher used by the Core agent's pattern-memory context provider.</param>
    /// <param name="loggerFactory">The logger factory used by client-level and pattern-memory logging.</param>
    /// <param name="registry">The MCP server registry for managing MCP server instances.</param>
    /// <param name="localTools">The in-process tools that <see cref="AgentToolSelection" /> can bind to an agent.</param>
    /// <param name="settings">Runtime SentinelCore settings that control logging and telemetry switches.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, IModelResolver modelResolver, IPatternMatcher patternMatcher, ILoggerFactory loggerFactory, IMcpServerRegistry registry, IEnumerable<AITool> localTools, SentinelCoreSettings settings)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _modelResolver = modelResolver ?? throw new ArgumentNullException(nameof(modelResolver));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<SentinelAgentFactory>();
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        ArgumentNullException.ThrowIfNull(localTools);
        _localTools = [.. localTools];

        SentinelCoreSettings runtimeSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        _enableChatClientLogging = runtimeSettings.ChatClientLoggingEnabled;
        _enableOpenTelemetryTracing = runtimeSettings.OpenTelemetryTracingEnabled;

        _builtToolCache = _registry.GetToolsFromCache();
        _registry.ToolCacheChanged += OnToolCacheChanged;

    }








    /// <summary>
    ///     Creates an agent from a preset using the preset's default toolset behaviour.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="cancellationToken">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default)
    {
        // 2‑parameter overload forwards to the 3‑parameter overload with no task instructions.
        return await CreateAgentAsync(presetName, (string?)null, cancellationToken);
    }








    /// <summary>
    ///     Creates an agent from a preset using the preset's default toolset behaviour.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="cancellationToken">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, CancellationToken cancellationToken = default)
    {
        // 3‑parameter overload forwards to the 4‑parameter overload with no JSON schema.
        return await CreateAgentAsync(presetName, taskInstructions, null, cancellationToken);
    }








    /// <summary>
    ///     Creates an agent from a preset, optionally producing JSON that conforms to a schema.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="forJsonSchema">When supplied, the agent must answer with JSON matching this schema.</param>
    /// <param name="token">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        // Forward to core implementation that performs all setup.
        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, forJsonSchema, null, token);
    }








    /// <summary>
    ///     Creates a named agent from a preset, overriding the preset's name and description.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        //edit the preset with the provided name and description
        preset.AgentName = agentName;
        preset.Description = agentDescription;


        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, null, null, CancellationToken.None);
    }




    /// <summary>
    ///     Creates a named agent from a preset, overriding the preset's name and description.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription)
    {
        _logger.LogInformation("Creating agent with preset: {PresetName}, name: {AgentName}, description: {AgentDescription}", presetName, agentName, agentDescription);

        AgentPresetDefinition? presetDefinition = AgentPresetDefinition.FromName(presetName);
        if (presetDefinition == null)
        {
            throw new InvalidOperationException($"Preset definition for {presetName} could not be resolved.");
        }

        presetDefinition.AgentName = agentName;
        presetDefinition.Description = agentDescription;

        return await CreateAgentCoreAsync(presetName, presetDefinition, null, null, null, CancellationToken.None);
    }




    /// <summary>
    ///     Creates an agent from a preset with an explicit tool selection.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <param name="cancellationToken">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolSelection);
        return await CreateAgentAsync(presetName, toolSelection, null, null, cancellationToken);
    }




    /// <summary>
    ///     Creates an agent from a preset with an explicit tool selection.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="cancellationToken">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolSelection);
        return await CreateAgentAsync(presetName, toolSelection, taskInstructions, null, cancellationToken);
    }




    /// <summary>
    ///     Creates an agent from a preset with an explicit tool selection, optionally producing JSON that
    ///     conforms to a schema.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="forJsonSchema">When supplied, the agent must answer with JSON matching this schema.</param>
    /// <param name="token">A token that cancels agent construction.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(toolSelection);
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, forJsonSchema, toolSelection, token);
    }




    /// <summary>
    ///     Creates a named agent from a preset with an explicit tool selection.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, AgentToolSelection toolSelection)
    {
        ArgumentNullException.ThrowIfNull(toolSelection);
        return await CreateAgentAsync(presetName, agentName, agentDescription, null, toolSelection);
    }




    /// <summary>
    ///     Creates a named agent from a preset with an explicit tool selection.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string? taskInstructions, AgentToolSelection toolSelection)
    {
        ArgumentNullException.ThrowIfNull(toolSelection);

        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        preset.AgentName = agentName;
        preset.Description = agentDescription;

        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, null, toolSelection, CancellationToken.None);
    }








    /// <summary>
    ///     Adds context providers specified by the agent preset.
    /// </summary>
    /// <param name="preset">The preset that determines which optional context providers apply.</param>
    /// <param name="contextProviders">The context-provider collection for the agent being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    private void AddPresetContextProviders(AgentPresetDefinition preset, List<AIContextProvider> contextProviders)
    {
        ArgumentNullException.ThrowIfNull(preset);
        contextProviders ??= [];

        /*
         TEMPORARILY DISABLED UNTIL PATTERN MEMORY IS STABLE

                if (preset.UsePatternMemory)
                {
                    contextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
                }

             //   PipelineCompactionStrategy pipeline = new(new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(0x200)), new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(25)), new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x128000)));

             //   contextProviders.Add(new CompactionProvider(pipeline));
        */
        if (preset.UseContextEnrichers)
        {
            // Add context enrichers here
        }
    }








    /// <summary>
    ///     Constructs the final <see cref="AIAgent" /> from the configured chat client and context providers.
    /// </summary>
    /// <param name="presetKind">The preset enum that structurally identifies the agent's role.</param>
    /// <param name="preset">The preset that supplies the agent identity.</param>
    /// <param name="toolSelection">
    ///     The call-site tool selection, or <c>null</c> to keep the preset-driven default toolset behaviour.
    /// </param>
    /// <param name="chatClient">The configured chat client that serves model requests.</param>
    /// <param name="chatResponseFormat">The response format the agent must produce, or <c>null</c> for JSON.</param>
    /// <param name="contextProviders">The context providers that enrich agent invocations.</param>
    /// <returns>A fully configured <see cref="ChatClientAgent" />.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the Manager preset is given a non-empty toolset, or when a named tool cannot be resolved.
    /// </exception>
    private ChatClientAgent BuildAgent(AgentPreset presetKind, AgentPresetDefinition preset, AgentToolSelection? toolSelection, IChatClient chatClient, ChatResponseFormat? chatResponseFormat = null, List<AIContextProvider?>? contextProviders = null)
    {

        IReadOnlyList<AITool> tools = ResolveTools(presetKind, toolSelection);
        EnsureManagerHasNoTools(presetKind, tools);



        ChatOptions chatOptions = new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = string.Concat(preset.DefaultSystemInstructions, preset.RoleInstructions), //Setting instructions at system level adds strength to them making them less likely to be ignored by agent, user level chat messages are suggestions in comparison.
            Temperature = preset.ModelProfile?.Temperature,
            MaxOutputTokens = preset.ModelProfile?.MaxOutputTokens ?? 45000,
            TopP = preset.ModelProfile?.TopP ?? .10f,
            TopK = preset.ModelProfile?.TopK ?? 30,
            Seed = 69,
            ModelId = preset.ModelProfile?.ModelId,
            AllowMultipleToolCalls = true,
            ToolMode = ChatToolMode.Auto,
            Tools = [.. tools],
            ResponseFormat = chatResponseFormat ?? ChatResponseFormat.Json
        };


        return new(chatClient, new ChatClientAgentOptions
        {
            Id = preset.AgentId,
            Name = preset.AgentName,
            Description = preset.Description,
            ChatOptions = chatOptions,
            AIContextProviders = preset.UseContextEnrichers ? contextProviders : null,
            UseProvidedChatClientAsIs = false,
            AllowConcurrentInvocation = false,
            ClearOnChatHistoryProviderConflict = false,
            WarnOnChatHistoryProviderConflict = false,
            ThrowOnChatHistoryProviderConflict = false,
            RequirePerServiceCallChatHistoryPersistence = false,
            EnableMessageInjection = false,
            DisableApprovalNotRequiredFunctionBypassing = false,
            DisableApprovalResponseBinding = false,
            ChatHistoryProvider = null
        });
    }








    /// <summary>
    ///     Resolves the tools that are bound to an agent for the supplied selection.
    /// </summary>
    /// <param name="presetKind">The preset enum that selects the role toolset used when no selection is supplied.</param>
    /// <param name="toolSelection">
    ///     The call-site tool selection, or <c>null</c> to bind the preset's role toolset.
    /// </param>
    /// <returns>The tools to bind to the agent. Never <c>null</c>.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved or the selection kind is not supported.
    /// </exception>
    private IReadOnlyList<AITool> ResolveTools(AgentPreset presetKind, AgentToolSelection? toolSelection)
    {
        if (toolSelection is null)
        {
            // No preset decides the toolset; the role vocabulary does. A tool-free role binds nothing.
            IReadOnlyList<AgentToolDomain> roleDomains = AgentToolRoleToolsets.For(presetKind);

            return roleDomains.Count == 0 ? [] : ResolveDomainTools(roleDomains);
        }

        switch (toolSelection.Kind)
        {
            case AgentToolSelectionKind.None:
                return [];
            case AgentToolSelectionKind.All:
                return [.. BuildToolCatalog().Values];
            case AgentToolSelectionKind.Named:
                return ResolveNamedTools(toolSelection.ToolNames);
            case AgentToolSelectionKind.Domains:
                return ResolveDomainTools(toolSelection.Domains);
            default:
                throw new InvalidOperationException($"Unsupported tool selection kind '{toolSelection.Kind}'.");
        }
    }




    /// <summary>
    ///     Builds the merged MCP and local tool catalog keyed by tool name.
    /// </summary>
    /// <returns>The merged catalog. The returned dictionary is not cached and is safe to mutate.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a local tool collides with any other tool, which is always a code defect.
    /// </exception>
    private Dictionary<string, AITool> BuildToolCatalog()
    {
        Dictionary<string, AITool> catalog = new(StringComparer.OrdinalIgnoreCase);

        foreach (AITool tool in _builtToolCache)
        {
            if (!catalog.TryAdd(tool.Name, tool))
            {
                // Two MCP servers advertising the same tool name is external configuration, not a
                // code defect, so the first registration wins instead of aborting the workflow.
                _logger.LogWarning(
                    "The MCP tool name '{ToolName}' is advertised by more than one MCP server; the first registration is used.",
                    tool.Name);
            }
        }

        foreach (AITool tool in _localTools)
        {
            if (!catalog.TryAdd(tool.Name, tool))
            {
                // A local tool that collides with any other tool is a code defect and must fail fast.
                throw new InvalidOperationException(
                    $"The local tool '{tool.Name}' collides with another MCP or local tool. Local tool names must be unique.");
            }
        }

        return catalog;
    }




    /// <summary>
    ///     Resolves an explicit list of tool names against the merged MCP and local tool catalog.
    /// </summary>
    /// <param name="toolNames">The case-insensitive tool names requested by the call site.</param>
    /// <returns>The resolved tools in the requested order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when any requested tool name is not registered.</exception>
    private IReadOnlyList<AITool> ResolveNamedTools(IReadOnlyList<string> toolNames)
    {
        Dictionary<string, AITool> catalog = BuildToolCatalog();
        List<AITool> resolved = new(toolNames.Count);
        List<string> unknown = [];

        foreach (string toolName in toolNames)
        {
            if (catalog.TryGetValue(toolName, out AITool? tool))
            {
                resolved.Add(tool);
            }
            else
            {
                unknown.Add(toolName);
            }
        }

        if (unknown.Count > 0)
        {
            string available = string.Join(", ", catalog.Keys.Order(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(
                $"The following tools are not registered in the MCP or local tool catalog: {string.Join(", ", unknown)}. Available tools: {available}.");
        }

        return resolved;
    }





    /// <summary>
    ///     Resolves the tools claimed by the supplied domains against the merged MCP and local tool catalogue.
    /// </summary>
    /// <param name="domains">The domains whose tools should be bound.</param>
    /// <returns>The union of the domain tools, de-duplicated by name and ordered for deterministic construction.</returns>
    /// <remarks>
    ///     Domains hold rules rather than fixed name lists, so a tool that is added later is claimed automatically.
    ///     An empty result is a soft warning, never a failure: a domain can legitimately be empty when the server
    ///     that owns it is not loaded, and the call site decides whether that matters.
    /// </remarks>
    private IReadOnlyList<AITool> ResolveDomainTools(IReadOnlyList<AgentToolDomain> domains)
    {
        Dictionary<string, AITool> catalog = BuildToolCatalog();
        HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);

        foreach (AgentToolDomain domain in domains)
        {
            foreach (string toolName in domain.SelectFrom(catalog.Keys))
            {
                _ = selected.Add(toolName);
            }
        }

        if (selected.Count == 0)
        {
            _logger.LogWarning(
                    "The domain tool selection [{Domains}] resolved to zero tools against a catalogue of {ToolCount} tool(s). The agent is built with an empty toolset.",
                    string.Join(", ", domains.Select(domain => domain.Name)),
                    catalog.Count);

            return [];
        }

        return [.. selected.Order(StringComparer.OrdinalIgnoreCase).Select(toolName => catalog[toolName])];
    }





    /// <summary>
    ///     Reports the catalogue tools that no domain claims so the vocabulary can be extended instead of
    ///     silently losing reachability.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to test.</param>
    private void ReportUnclassifiedTools(IEnumerable<string> toolNames)
    {
        IReadOnlyList<string> unclassified = AgentToolDomainCatalog.Unclassified(toolNames);

        if (unclassified.Count > 0)
        {
            _logger.LogWarning(
                    "{Count} tool(s) are not claimed by any tool domain: {Tools}. They remain reachable through AgentToolSelection.All and by explicit name.",
                    unclassified.Count,
                    string.Join(", ", unclassified));
        }
    }




    /// <summary>
    ///     Enforces PL-3: the Manager agent must never be given a toolset.
    /// </summary>
    /// <param name="presetKind">The preset enum that identifies the agent's role.</param>
    /// <param name="tools">The tools that were resolved for the agent.</param>
    /// <exception cref="InvalidOperationException">Thrown when the Manager preset is given a non-empty toolset.</exception>
    private static void EnsureManagerHasNoTools(AgentPreset presetKind, IReadOnlyList<AITool> tools)
    {
        if (presetKind == AgentPreset.ManagerAgent && tools.Count > 0)
        {
            throw new InvalidOperationException(
                "PL-3 forbids tools on the Manager agent. The Manager only coordinates the magnetic orchestration workflow and must never reason or call tools.");
        }
    }




    /// <summary>
    ///     Shared implementation for creating an <see cref="AIAgent" /> from a preset definition.
    ///     Handles model resolution, chat client construction with logging, and optional context providers.
    /// </summary>
    /// <param name="presetKind">The preset enum that structurally identifies the agent's role.</param>
    /// <param name="preset">The resolved preset definition that supplies the agent identity and defaults.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="forJsonSchema">When supplied, the agent must answer with JSON matching this schema.</param>
    /// <param name="toolSelection">
    ///     The call-site tool selection, or <c>null</c> to keep the preset-driven default toolset behaviour.
    /// </param>
    /// <param name="token">A token that cancels agent construction.</param>
    /// <returns>A fully configured <see cref="AIAgent" />.</returns>
    private async Task<AIAgent> CreateAgentCoreAsync(AgentPreset presetKind, AgentPresetDefinition preset, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, AgentToolSelection? toolSelection, CancellationToken token = default)
    {
        // Resolve the appropriate model profile based on the preset's tier
        ModelProfile? model = _modelResolver.ResolveByTier(preset.Tier);
        preset.ModelProfile = model;
        Throw.IfNull(model, $"Unable to resolve a model for preset '{preset.Tier}'.");



        // Build the base chat client and wrap with logging middleware
        IChatClient chatclient = _chatClientFactory.CreateChatClient(model);


        // Prepare context providers when the preset requests any enrichers.
        List<AIContextProvider?> providers = null!;
        if (preset.UseContextEnrichers || preset.UsePatternMemory || preset.UseRagSearch)
        {
            providers = [];
            AddPresetContextProviders(preset, providers);
        }



        AIAgent builtAgent;
        if (_enableChatClientLogging)
        {
            // =====  USE ONLY FOR TESTING AGENT BEHAVIOR, NOT FOR PRODUCTION LOGGING =====
            IChatClient loggingClient = chatclient.AsBuilder().UseLogging(_loggerFactory).Build();
            // Build and cast to the public interface type.
            builtAgent = BuildAgent(presetKind, preset, toolSelection, loggingClient, forJsonSchema, providers);
        }
        else
        {
            // Build and cast to the public interface type.
            builtAgent = BuildAgent(presetKind, preset, toolSelection, chatclient, forJsonSchema, providers);
        }



        if (_enableOpenTelemetryTracing)
        {
            builtAgent = builtAgent.AsBuilder()
                    .UseOpenTelemetry(SentinelCoreDiagnostics.ActivitySourceName, cfg => cfg.EnableSensitiveData = SentinelCoreDiagnostics.IsSensitiveDataCaptureEnabled).Build();
        }


        return builtAgent;
    }








    /// <summary>
    ///     Handles a change notification from the MCP server registry by refreshing the tool cache.
    /// </summary>
    private void OnToolCacheChanged()
    {
        RefreshToolCache();
    }








    /// <summary>
    ///     Refreshes the tool cache by querying the MCP server registry for the latest tools.
    ///     This should be called when the MCP server registry changes (e.g., after servers are started).
    /// </summary>
    public void RefreshToolCache()
    {
        _builtToolCache = _registry.GetToolsFromCache();

        List<string> catalogNames = [.. _builtToolCache.Select(tool => tool.Name), .. _localTools.Select(tool => tool.Name)];
        ReportUnclassifiedTools(catalogNames);
    }








    /// <summary>
    ///     Registers a worker agent with the next available positive numeric suffix.
    /// </summary>
    /// <param name="agentId">The base identifier of the worker agent.</param>
    /// <param name="agentName">The base display name of the worker agent.</param>
    /// <returns>The unique agent identifier and display name assigned to the worker.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentId" /> or <paramref name="agentName" /> is <c>null</c>.</exception>
    private (string, string) RegisterUniqueWorkerAgent(string agentId, string agentName)
    {
        ArgumentNullException.ThrowIfNull(agentId);
        ArgumentNullException.ThrowIfNull(agentName);

        lock (_builtAgentsLock)
        {
            int suffix = 1;


            do
            {
                agentId = $"{agentId}{suffix}";
                agentName = $"{agentName}{suffix}";
                suffix++;
            } while (_builtAgents.ContainsKey(agentId) || _builtAgents.ContainsValue(agentName));

            _builtAgents.Add(agentId, agentName);
            return (agentId, agentName);
        }
    }
}
