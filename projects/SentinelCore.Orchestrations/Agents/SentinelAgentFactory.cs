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
using SentinelCore.Orchestrations.Agents.Core.Tools;
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
///         Instructions are composed at construction from the preset (default system + role instructions) plus
///         optional task instructions supplied by the call site; response format is enforced only when a call
///         site supplies one, otherwise the model default applies.
///     </para>
/// </summary>
public sealed class SentinelAgentFactory : ISentinelAgentFactory
{
    private readonly Dictionary<string, string> _builtAgents = new(StringComparer.Ordinal);
    private readonly Lock _builtAgentsLock = new();
    private IReadOnlyList<AITool> _builtToolCache;
    private readonly IReadOnlyList<AITool> _localTools;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly bool _enableChatClientLogging;
    private readonly bool _enableOpenTelemetryTracing;
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
    /// <param name="modelResolver">Resolves the model profile for a preset's model tier.</param>
    /// <param name="patternMatcher">The pattern matcher used by the Core agent's pattern-memory context provider.</param>
    /// <param name="loggerFactory">The logger factory used by client-level and pattern-memory logging.</param>
    /// <param name="registry">The MCP server registry for managing MCP server instances.</param>
    /// <param name="localTools">The in-process tools registered in dependency injection that agents can bind alongside MCP-server tools.</param>
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

        List<string> toolNames = [.. _builtToolCache.Select(tool => tool.Name), .. _localTools.Select(tool => tool.Name)];
        _logger.LogInformation("SentinelAgentFactory initialized with {ToolCount} tool(s): {ToolNames}", toolNames.Count, string.Join(", ", toolNames));
        ReportNonConformingTools(toolNames);
    }








    /// <summary>
    ///     Creates an agent from the preset's default configuration: default name, description and preset tool groups.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default)
    {
        // 2‑parameter overload forwards to the 3‑parameter overload with no task instructions.
        return await CreateAgentAsync(presetName, (string?)null, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Creates an agent from the preset's default configuration, appending task-specific instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, CancellationToken cancellationToken = default)
    {
        // 3‑parameter overload forwards to the 4‑parameter overload with no JSON schema.
        return await CreateAgentAsync(presetName, taskInstructions, null, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Creates an agent from the preset's default configuration, appending task-specific instructions and
    ///     constraining responses to a JSON schema.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="forJsonSchema">The JSON response format to enforce; may be <c>null</c> for free-form text.</param>
    /// <param name="token">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        // Forward to core implementation that performs all setup. The preset's default tool groups apply.
        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, null, forJsonSchema, token).ConfigureAwait(false);
    }








    /// <summary>
    ///     Creates an agent from the preset's default configuration with an explicit name and description, appending
    ///     task-specific instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and default tool groups.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        //edit the preset with the provided name and description
        preset.AgentName = agentName;
        preset.Description = agentDescription;


        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, null, null, CancellationToken.None).ConfigureAwait(false);
    }








    /// <summary>
    ///     Creates an agent from the preset's default configuration with an explicit name and description.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and default tool groups.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription)
    {
        AgentPresetDefinition presetDefinition = AgentPresetDefinition.FromName(presetName);
        presetDefinition.AgentName = agentName;
        presetDefinition.Description = agentDescription;

        return await CreateAgentCoreAsync(presetName, presetDefinition, null, null, null, CancellationToken.None).ConfigureAwait(false);
    }







    /// <summary>
    ///     Creates an agent from the preset with an explicit tool selection overriding the preset's tool groups.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, CancellationToken cancellationToken = default)
    {
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);

        return await CreateAgentCoreAsync(presetName, preset, null, toolSelection, null, cancellationToken).ConfigureAwait(false);
    }







    /// <summary>
    ///     Creates an agent from the preset with an explicit tool selection, appending task-specific instructions and
    ///     optionally constraining responses to a JSON schema.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="forJsonSchema">The JSON response format to enforce; may be <c>null</c> for free-form text.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, ChatResponseFormat? forJsonSchema = null, CancellationToken cancellationToken = default)
    {
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);

        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, toolSelection, forJsonSchema, cancellationToken).ConfigureAwait(false);
    }







    /// <summary>
    ///     Creates an agent from the preset with an explicit name, description and tool selection.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, AgentToolSelection toolSelection)
    {
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        preset.AgentName = agentName;
        preset.Description = agentDescription;

        return await CreateAgentCoreAsync(presetName, preset, null, toolSelection, null, CancellationToken.None).ConfigureAwait(false);
    }







    /// <summary>
    ///     Creates an agent from the preset with an explicit name, description, tool selection and task instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <returns>The constructed agent.</returns>
    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string? taskInstructions, AgentToolSelection toolSelection)
    {
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        preset.AgentName = agentName;
        preset.Description = agentDescription;

        return await CreateAgentCoreAsync(presetName, preset, taskInstructions, toolSelection, null, CancellationToken.None).ConfigureAwait(false);
    }








    /// <summary>
    ///     Adds context providers specified by the agent preset.
    /// </summary>
    /// <param name="preset">The preset that determines which optional context providers apply.</param>
    /// <param name="contextProviders">The context-provider collection for the agent being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    private void AddPresetContextProviders(AgentPresetDefinition preset, List<AIContextProvider?> contextProviders)
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
    ///     Constructs the final <see cref="AIAgent" /> from the configured chat client, resolved toolset, task
    ///     instructions and context providers.
    /// </summary>
    /// <param name="presetKind">The preset enum that identifies the agent's role, used for role invariants.</param>
    /// <param name="preset">The preset that supplies the agent identity.</param>
    /// <param name="chatClient">The configured chat client that serves model requests.</param>
    /// <param name="toolSelection">The call-site tool selection, or <c>null</c> to bind the preset's default tool groups.</param>
    /// <param name="chatResponseFormat">The JSON response format to enforce, or <c>null</c> for free-form text.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="contextProviders">The context providers that enrich agent invocations.</param>
    /// <returns>A fully configured <see cref="ChatClientAgent" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a role invariant such as PL-3 is violated.</exception>
    private ChatClientAgent BuildChatClientAgent(AgentPreset presetKind, AgentPresetDefinition preset, IChatClient chatClient, AgentToolSelection? toolSelection, ChatResponseFormat? chatResponseFormat, string? taskInstructions, List<AIContextProvider?>? contextProviders = null)
    {
        IReadOnlyList<AITool> tools = ResolveTools(presetKind, preset, toolSelection);
        EnsureManagerHasNoTools(presetKind, tools);

        string instructions = string.Concat(preset.DefaultSystemInstructions, preset.RoleInstructions, taskInstructions); //Setting instructions at system level adds strength to them making them less likely to be ignored by agent, user level chat messages are suggestions in comparison.

        ChatOptions chatOptions = new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = instructions,
            Temperature = preset.ModelProfile?.Temperature,
            MaxOutputTokens = preset.ModelProfile?.MaxOutputTokens ?? 45000,
            TopP = preset.ModelProfile?.TopP ?? .10f,
            TopK = preset.ModelProfile?.TopK ?? 30,
            Seed = 69,
            ModelId = preset.ModelProfile?.ModelId,
            AllowMultipleToolCalls = true,
            ToolMode = ChatToolMode.Auto,
            Tools = [.. tools],
            ResponseFormat = chatResponseFormat
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
    ///     Shared implementation for creating an <see cref="AIAgent" /> from a preset definition.
    ///     Handles model resolution, chat client construction with logging, toolset resolution, and optional context providers.
    /// </summary>
    /// <param name="presetKind">The preset enum that identifies the agent's role, used for role invariants.</param>
    /// <param name="preset">The preset definition resolved from <paramref name="presetKind" />; name and description may already be overridden by the call site.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="toolSelection">The call-site tool selection, or <c>null</c> to bind the preset's default tool groups.</param>
    /// <param name="forJsonSchema">The JSON response format to enforce, or <c>null</c> for free-form text.</param>
    /// <param name="token">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    private async Task<AIAgent> CreateAgentCoreAsync(AgentPreset presetKind, AgentPresetDefinition preset, string? taskInstructions, AgentToolSelection? toolSelection, ChatResponseFormat? forJsonSchema, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(preset);

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


        _logger.LogInformation(
                "Creating agent '{AgentName}' from preset {PresetKind} with tool selection {ToolSelection}.",
                preset.AgentName,
                presetKind,
                toolSelection?.ToString() ?? "preset default");

        AIAgent builtAgent;
        if (_enableChatClientLogging)
        {
            // =====  USE ONLY FOR TESTING AGENT BEHAVIOR, NOT FOR PRODUCTION LOGGING =====
            IChatClient loggingClient = chatclient.AsBuilder().UseLogging(_loggerFactory).Build();
            // Build and cast to the public interface type.
            builtAgent = BuildChatClientAgent(presetKind, preset, loggingClient, toolSelection, forJsonSchema, taskInstructions, providers);
        }
        else
        {
            // Build and cast to the public interface type.
            builtAgent = BuildChatClientAgent(presetKind, preset, chatclient, toolSelection, forJsonSchema, taskInstructions, providers);
        }



        if (_enableOpenTelemetryTracing)
        {
            builtAgent = builtAgent.AsBuilder().UseOpenTelemetry(SentinelCoreDiagnostics.ActivitySourceName, cfg => cfg.EnableSensitiveData = SentinelCoreDiagnostics.IsSensitiveDataCaptureEnabled).Build();
        }


        _logger.LogInformation("Agent '{AgentName}' created successfully.", preset.AgentName);
        return builtAgent;
    }

















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








    /// <summary>
    ///     Resolves the tools that are bound to an agent for the supplied preset and selection.
    /// </summary>
    /// <param name="presetKind">The preset enum that identifies the agent's role, used for role invariants.</param>
    /// <param name="preset">The preset whose default tool groups apply when no selection is supplied.</param>
    /// <param name="toolSelection">The call-site tool selection, or <c>null</c> to bind the preset's default tool groups.</param>
    /// <returns>The tools to bind to the agent. Never <c>null</c>; possibly empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the selection kind is not supported.</exception>
    private IReadOnlyList<AITool> ResolveTools(AgentPreset presetKind, AgentPresetDefinition preset, AgentToolSelection? toolSelection)
    {
        ArgumentNullException.ThrowIfNull(preset);

        if (toolSelection is null)
        {
            // No selection passed: the preset decides the toolset. An empty group list binds nothing silently,
            // which is how tool-free roles such as the Manager are expressed.
            IReadOnlyList<string> presetGroups = preset.ToolGroups;

            return presetGroups.Count == 0 ? [] : ResolveGroupTools(presetGroups);
        }

        return toolSelection.Kind switch
        {
            AgentToolSelectionKind.None => [],
            AgentToolSelectionKind.All => ResolveAllTools(),
            AgentToolSelectionKind.Named => ResolveNamedTools(toolSelection.ToolNames),
            AgentToolSelectionKind.Groups => ResolveGroupTools(toolSelection.Groups),
            _ => throw new InvalidOperationException($"Unsupported tool selection kind '{toolSelection.Kind}'.")
        };
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
                _logger.LogWarning("The MCP tool name '{ToolName}' is advertised by more than one MCP server; the first registration is used.", tool.Name);
            }
        }

        foreach (AITool tool in _localTools)
        {
            if (!catalog.TryAdd(tool.Name, tool))
            {
                // A local tool that collides with any other tool is a code defect and must fail fast.
                throw new InvalidOperationException($"The local tool '{tool.Name}' collides with another MCP or local tool. Local tool names must be unique.");
            }
        }

        return catalog;
    }








    /// <summary>
    ///     Resolves an explicit list of tool names against the merged MCP and local tool catalog.
    /// </summary>
    /// <param name="toolNames">The case-insensitive tool names requested by the call site.</param>
    /// <returns>The resolved tools in the requested order, skipping tools that cannot be bound.</returns>
    /// <remarks>
    ///     <para>
    ///         An unknown name is logged as a warning and skipped rather than thrown: name-based resolution is a
    ///         development convenience and a missing name must not abort agent construction.
    ///     </para>
    ///     <para>
    ///         A suspect name — it carries the <c>sc_</c> root but no registered group claims it — is logged as an
    ///         error with maximum detail and is never bound: it is either a naming defect or a malicious tool, and
    ///         suspect tools are unreachable by design.
    ///     </para>
    ///     <para>
    ///         An uncategorized name — foreign to the naming convention — is bound when explicitly requested so the
    ///         call site decides whether to use it.
    ///     </para>
    /// </remarks>
    private IReadOnlyList<AITool> ResolveNamedTools(IReadOnlyList<string> toolNames)
    {
        Dictionary<string, AITool> catalog = BuildToolCatalog();
        List<AITool> resolved = new(toolNames.Count);

        foreach (string toolName in toolNames)
        {
            if (!catalog.TryGetValue(toolName, out AITool? tool))
            {
                string available = string.Join(", ", catalog.Keys.Order(StringComparer.OrdinalIgnoreCase));
                _logger.LogWarning("The requested tool '{ToolName}' is not registered in the MCP or local tool catalog and is skipped. Available tools: {AvailableTools}.", toolName, available);
                continue;
            }

            if (AgentToolGroupCatalog.IsSuspect(tool.Name))
            {
                _logger.LogError(
                        "Refusing to bind suspect tool '{ToolName}': the name carries the '{PrefixRoot}' root but no registered tool group claims it. The tool may be misnamed on the MCP server or malicious. Suspect tools are excluded from every grouping and from resolution by name, so they are unreachable. Registered groups: {RegisteredGroups}.",
                        tool.Name,
                        AgentToolGroup.NamePrefixRoot,
                        string.Join(", ", AgentToolGroupCatalog.Names));
                continue;
            }

            resolved.Add(tool);
        }

        return resolved;
    }








    /// <summary>
    ///     Resolves every tool claimed by a registered tool group prefix against the merged MCP and local tool catalogue.
    /// </summary>
    /// <returns>The claimed tools ordered for deterministic construction, or an empty toolset when nothing is claimed.</returns>
    /// <remarks>
    ///     The <see cref="AgentToolSelectionKind.All" /> selection is a debug catch-all: it binds only the tools that
    ///     carry a registered group prefix, never the uncategorized or suspect tools.
    /// </remarks>
    private IReadOnlyList<AITool> ResolveAllTools()
    {
        Dictionary<string, AITool> catalog = BuildToolCatalog();
        IReadOnlyList<string> claimed = AgentToolGroupCatalog.Claimed(catalog.Keys);

        if (claimed.Count == 0)
        {
            _logger.LogWarning("The {SelectionKind} tool selection resolved to zero tools against a catalogue of {ToolCount} tool(s). The agent is built with an empty toolset.", AgentToolSelectionKind.All, catalog.Count);

            return [];
        }

        return [.. claimed.Order(StringComparer.OrdinalIgnoreCase).Select(toolName => catalog[toolName])];
    }







    /// <summary>
    ///     Resolves the tools claimed by the supplied group names against the merged MCP and local tool catalogue.
    /// </summary>
    /// <param name="groupNames">The case-insensitive group names whose tools should be bound.</param>
    /// <returns>The union of the group tools, de-duplicated by name and ordered for deterministic construction.</returns>
    /// <remarks>
    ///     An unknown group name is a preset or call-site configuration defect and throws. A registered group whose
    ///     prefix claims zero live tools is a soft warning: the group can legitimately be empty when the MCP server
    ///     that owns it is not loaded, and the agent is built without that group's tools.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when a group name is not registered.</exception>
    private IReadOnlyList<AITool> ResolveGroupTools(IReadOnlyList<string> groupNames)
    {
        if (groupNames.Count == 0)
        {
            return [];
        }

        Dictionary<string, AITool> catalog = BuildToolCatalog();
        List<AgentToolGroup> groups = new(groupNames.Count);

        foreach (string groupName in groupNames)
        {
            groups.Add(AgentToolGroupCatalog.FindRequired(groupName));
        }

        HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);

        foreach (string toolName in catalog.Keys)
        {
            if (groups.Any(group => group.Claims(toolName)))
            {
                _ = selected.Add(toolName);
            }
        }

        if (selected.Count == 0)
        {
            _logger.LogWarning("The group tool selection [{Groups}] resolved to zero tools against a catalogue of {ToolCount} tool(s). The agent is built with an empty toolset.", string.Join(", ", groupNames), catalog.Count);

            return [];
        }

        return [.. selected.Order(StringComparer.OrdinalIgnoreCase).Select(toolName => catalog[toolName])];
    }








    /// <summary>
    ///     Reports catalogue tools that do not conform to the tool group naming scheme so the condition is visible
    ///     instead of silently changing reachability.
    /// </summary>
    /// <param name="toolNames">The live catalogue of tool names to test.</param>
    /// <remarks>
    ///     Uncategorized tools (foreign to the naming convention) are a warning and stay reachable by explicit name.
    ///     Suspect tools (they carry the <c>sc_</c> root but no registered group claims them) are an error with as
    ///     much detail as possible: the condition may be a typo, but it may also indicate a malicious tool added to
    ///     the MCP server, and suspect tools are excluded from every grouping and from resolution by name.
    /// </remarks>
    private void ReportNonConformingTools(IEnumerable<string> toolNames)
    {
        IReadOnlyList<string> uncategorized = AgentToolGroupCatalog.Uncategorized(toolNames);

        if (uncategorized.Count > 0)
        {
            _logger.LogWarning("{Count} tool(s) carry no registered tool group prefix: {Tools}. They are excluded from every grouping and remain reachable by explicit name only.", uncategorized.Count, string.Join(", ", uncategorized));
        }

        IReadOnlyList<string> suspect = AgentToolGroupCatalog.Suspect(toolNames);

        if (suspect.Count > 0)
        {
            _logger.LogError(
                    "{Count} suspect tool(s) carry the '{PrefixRoot}' root but no registered tool group claims them: {Tools}. They may be misnamed on the MCP server or malicious. Suspect tools are excluded from every grouping and from resolution by name, so they are unreachable. Registered groups: {RegisteredGroups}.",
                    suspect.Count,
                    AgentToolGroup.NamePrefixRoot,
                    string.Join(", ", suspect),
                    string.Join(", ", AgentToolGroupCatalog.Names));
        }
    }








    /// <summary>
    ///     Enforces PL-3: the Manager agent must never be given a toolset.
    /// </summary>
    /// <param name="presetKind">The preset enum that identifies the agent's role.</param>
    /// <param name="tools">The tools that were resolved for the agent.</param>
    /// <exception cref="InvalidOperationException">Thrown when the Manager preset is given a non-empty toolset.</exception>
    /// <remarks>
    ///     The preset already declares no tool groups for the Manager; this guard catches a call site that overrides
    ///     the preset with a non-empty selection. Enforced both by the preset and by this factory guard, per the
    ///     tooling contract.
    /// </remarks>
    private static void EnsureManagerHasNoTools(AgentPreset presetKind, IReadOnlyList<AITool> tools)
    {
        if (presetKind == AgentPreset.ManagerAgent && tools.Count > 0)
        {
            throw new InvalidOperationException("PL-3 forbids tools on the Manager agent. The Manager only coordinates the magnetic orchestration workflow and must never reason or call tools.");
        }
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
    /// <remarks>
    ///     The refresh logs the merged tool count and names, and reports uncategorized and suspect tools so that the
    ///     naming-scheme conformance of the live catalogue is always visible after a change.
    /// </remarks>
    public void RefreshToolCache()
    {
        _builtToolCache = _registry.GetToolsFromCache();

        List<string> catalogNames = [.. _builtToolCache.Select(tool => tool.Name), .. _localTools.Select(tool => tool.Name)];
        _logger.LogInformation("Tool cache refreshed: {ToolCount} tool(s) available: {ToolNames}", catalogNames.Count, string.Join(", ", catalogNames));
        ReportNonConformingTools(catalogNames);
    }










}
