---
title: "Dynamic Agents Component"
status: Active
component: DynamicAgents
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Dynamic Agents Component

**Project:** `SentinelCore.Orchestrations`
**Namespaces:** `SentinelCore.Orchestrations.Agents`, `SentinelCore.Orchestrations.Personas`
**Dependencies:** `SentinelCore.Contracts` (AgentRole, AgentSpec, ModelSettings, SentinelCoreSettings), `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`
**Consumers:** `SentinelCore.Orchestrations` (orchestrators, factories), `SentinelCoreHost` (via DI)

---

## Purpose

The Dynamic Agents component defines **how agents are specified, constructed, and personalized** in SentinelCore. It provides the two-stage construction pipeline (`IAgentSpecBuilder` → `IAgentBuilder`), the immutable `AgentSpec` record, the 6-role `AgentRole` enum, and the 35-persona `PersonaRegistry`. This component is the **agent identity and configuration layer** — every agent in the system flows through this pipeline.

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Dynamic Agents Component                      │  │
│  │  ┌──────────────────┐  ┌────────────────────────────────┐  │  │
│  │  │ IAgentSpecBuilder │──▶│ AgentSpec (immutable record)  │  │  │
│  │  │ AgentSpecBuilder  │    │ Role, Name, Persona, Tools,  │  │  │
│  │  └──────────────────┘    │ Model                          │  │  │
│  │           │              └──────────────┬────────────────┘  │  │
│  │           │                             │                   │  │
│  │           ▼                             ▼                   │  │
│  │  ┌──────────────────┐  ┌────────────────────────────────┐  │  │
│  │  │ PersonaRegistry  │  │ IAgentBuilder / AgentBuilder   │  │  │
│  │  │ (35 PersonaType) │  │ (OllamaClient → Logging →      │  │  │
│  │  └──────────────────┘  │  EventPub → ChatClientAgent)   │  │  │
│  │                        └──────────────┬────────────────┘  │  │
│  │                                       │                   │  │
│  │  ┌──────────────────┐                 │                   │  │
│  │  │ AgentRole (6)    │                 ▼                   │  │
│  │  │ Core, Manager,   │          AIAgent (configured)       │  │
│  │  │ Domain, Worker,  │                                       │  │
│  │  │ General, Agg     │                                       │  │
│  │  └──────────────────┘                                       │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ depends on
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  AgentRole, AgentSpec, ModelSettings, SentinelCoreSettings,     │
│  IAgentBuilder, IAgentSpecBuilder, IAgentPersona,               │
│  ISentinelCoreEvents, SafetyEngine contracts                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. AgentRole — The Single Source of Truth

**File:** `Agents/AgentRole.cs`
**Purpose:** Identifies the role an agent plays. Determines event routing, middleware, default persona, model settings, and tool set.

```csharp
public enum AgentRole
{
    /// <summary>The Core reasoning agent — application lifetime, research & case tools.</summary>
    Core,

    /// <summary>The Magnetic Orchestration Manager — workflow lifetime, no tools.</summary>
    Manager,

    /// <summary>A predefined Domain agent — per-task lifetime, domain-specific toolbelt.</summary>
    Domain,

    /// <summary>A multi-domain agent for cooperative workflows — per-task lifetime, general toolbelt.</summary>
    Worker,

    /// <summary>A general purpose agent — per-task lifetime.</summary>
    General,

    /// <summary>An aggregator agent — per-task lifetime, responsible for aggregating results.</summary>
    Aggregator
}
```

### Role Responsibilities Summary

| Role | Lifetime | Tools | Event Channel | Persona |
|------|----------|-------|---------------|---------|
| `Core` | Application | Role defaults + MCP (factory) | `TheCoreActivity` / `TheCoreReasoning` / `TheCoreTooling` | `TheCore` |
| `Manager` | Workflow | **None** | `MagneticParticipantActivity` | `TheManager` |
| `Domain` | Per-task | Domain-specific (30 domains) | `MagneticParticipantActivity` | `TheAnalyst` |
| `Worker` | Per-task | General (5 tools) | `MagneticParticipantActivity` | `TheWorker` |
| `General` | Per-task | Basic (3 tools) | `MagneticParticipantActivity` | `TheProblemSolver` |
| `Aggregator` | Per-task | **None** | `MagneticParticipantActivity` | `TheAggregator` |

---

## 2. AgentSpec — Immutable Agent Specification

**File:** `Agents/AgentSpec.cs`
**Pattern:** `sealed record` — fully immutable, with-expressions for overrides

```csharp
public sealed record AgentSpec
{
    /// <summary>The role this agent plays — determines event routing, middleware, defaults.</summary>
    public AgentRole Role { get; init; }

    /// <summary>The agent name (e.g. "TheCore", "registry_agent").</summary>
    public string AgentName { get; init; } = string.Empty;

    /// <summary>The persona providing system instructions and description.</summary>
    public AgentPersona Persona { get; init; } = new();

    /// <summary>The read-only list of tools available to the agent. May be empty.</summary>
    public IReadOnlyList<AITool> Tools { get; init; } = [];

    /// <summary>The model endpoint configuration for this agent.</summary>
    public ModelSettings Model { get; init; } = new("http://127.0.0.1:11434", string.Empty);
}
```

### Construction Flow

```
AgentRole
    │
    ▼
AgentSpecBuilder.BuildAgentSpec(role)
    │
    ├─▶ GetAgentName(role)           → "TheCore", "TheManager", etc.
    ├─▶ GetDefaultPersonaType(role)  → PersonaType.TheCore, TheManager, etc.
    ├─▶ PersonaRegistry.Get(type)    → AgentPersona (Name, Description, Instructions)
    ├─▶ ResolveModel(role)           → ModelSettings from SentinelCoreSettings
    ├─▶ ToolRegistry.GetToolsetByRole(role) → IReadOnlyList<AITool>
    │
    ▼
AgentSpec { Role, AgentName, Persona, Tools, Model }
    │
    ▼
IAgentBuilder.Build(spec) → AIAgent
```

### Factory Override Pattern (with-expressions)

```csharp
// CoreAgentFactory: adds MCP tools
AgentSpec spec = baseSpec with { Tools = [.. baseSpec.Tools, .. coreTools] };

// DomainAgentFactory: overrides name, tools, description
AgentSpec spec = baseSpec with
{
    AgentName = $"{domain}_agent",
    Tools = tools,
    Persona = string.IsNullOrWhiteSpace(description)
        ? baseSpec.Persona
        : baseSpec.Persona with { Description = description }
};
```

---

## 3. AgentSpecBuilder — Role-Based Spec Assembly

**File:** `Agents/AgentSpecBuilder.cs`
**Interface:** `IAgentSpecBuilder` (registered as singleton)
**Dependencies:** `IOptions<SentinelCoreSettings>`

### Constructor

```csharp
public AgentSpecBuilder(IOptions<SentinelCoreSettings> settings)
{
    _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
}
```

### Public API

```csharp
public AgentSpec BuildAgentSpec(AgentRole role)
    => BuildAgentSpec(role, GetDefaultPersona(role));

public AgentSpec BuildAgentSpec(AgentRole role, AgentPersona persona)
{
    ArgumentNullException.ThrowIfNull(persona);

    string agentName = GetAgentName(role);
    ModelSettings model = ResolveModel(role);
    IReadOnlyList<AITool> tools = ToolRegistry.GetToolsetByRole(role);

    return new AgentSpec
    {
        Role = role,
        AgentName = agentName,
        Persona = persona,
        Tools = tools,
        Model = model
    };
}
```

### Role → Name Mapping (`GetAgentName`)

```csharp
private static string GetAgentName(AgentRole role) => role switch
{
    AgentRole.Core => "TheCore",
    AgentRole.Manager => "TheManager",
    AgentRole.Domain => "TheDomainAgent",
    AgentRole.Worker => "TheWorkerAgent",
    AgentRole.General => "TheGeneralAgent",
    AgentRole.Aggregator => "TheAggregatorAgent",
    _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unsupported agent role: {role}")
};
```

### Role → PersonaType Mapping (`GetDefaultPersonaType`)

```csharp
private static PersonaType GetDefaultPersonaType(AgentRole role) => role switch
{
    AgentRole.Core => PersonaType.TheCore,
    AgentRole.Manager => PersonaType.TheManager,
    AgentRole.Domain => PersonaType.TheAnalyst,
    AgentRole.Worker => PersonaType.TheWorker,
    AgentRole.General => PersonaType.TheProblemSolver,
    AgentRole.Aggregator => PersonaType.TheAggregator,
    _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unsupported agent role: {role}")
};
```

### Role → ModelSettings Resolution (`ResolveModel`)

```csharp
private ModelSettings ResolveModel(AgentRole role) => role switch
{
    AgentRole.Core => _settings.CoreModel ?? _settings.DefaultModel
        ?? throw new InvalidOperationException("CoreModel or DefaultModel must be configured."),
    AgentRole.Manager => _settings.ManagerModel ?? _settings.DefaultModel
        ?? throw new InvalidOperationException("ManagerModel or DefaultModel must be configured."),
    AgentRole.Domain => _settings.DomainModel ?? _settings.DefaultModel
        ?? throw new InvalidOperationException("DomainModel or DefaultModel must be configured."),
    AgentRole.Worker => _settings.DefaultModel
        ?? throw new InvalidOperationException("DefaultModel must be configured for Worker agents."),
    AgentRole.General => _settings.DefaultModel
        ?? throw new InvalidOperationException("DefaultModel must be configured for General agents."),
    AgentRole.Aggregator => _settings.DefaultModel
        ?? throw new InvalidOperationException("DefaultModel must be configured for Aggregator agents."),
    _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unsupported agent role: {role}")
};
```

**Fallback Chain:** Role-specific model → `DefaultModel` → **Exception** (fail-fast)

---

## 4. PersonaRegistry — 35 Predefined Personas

**File:** `Personas/PersonaFactory.cs`
**Pattern:** Static `Dictionary<PersonaType, AgentPersona>` with `Get(PersonaType)` lookup

### PersonaType Enum (35 Values)

```csharp
public enum PersonaType
{
    TheDotnetExpert,
    TheCore,
    TheWorker,
    TheArchitect,
    TheEngineer,
    TheAnalyst,
    TheDesigner,
    TheManager,
    TheConsultant,
    TheStrategist,
    TheVisionary,
    TheInnovator,
    TheLeader,
    TheMentor,
    TheCoach,
    TheAdvisor,
    TheFacilitator,
    TheProblemSolver,
    TheDecisionMaker,
    TheCommunicator,
    TheCollaborator,
    TheNegotiator,
    TheInfluencer,
    ThePlanner,
    TheOrganizer,
    TheResearcher,
    TheEvaluator,
    TheImplementer,
    TheTester,
    TheMaintainer,
    TheSupporter,
    TheTrainer,
    TheEducator,
    TheMotivator,
    TheInspirer,
    TheDomainInvestigator,
    TheCritic,
    TheAggregator
}
```

### Role → PersonaType Assignments

| AgentRole | PersonaType | Purpose |
|-----------|-------------|---------|
| `Core` | `TheCore` | Senior forensic investigator |
| `Manager` | `TheManager` | Magnetic orchestration coordinator |
| `Domain` | `TheAnalyst` | Focused evidence gatherer |
| `Worker` | `TheWorker` | Pragmatic task executor |
| `General` | `TheProblemSolver` | Ambiguity resolver |
| `Aggregator` | `TheAggregator` | Result synthesizer |

### Key Persona Definitions

#### `TheCore` (Core Agent)
```csharp
Name = "TheCore"
Description = "Core reasoning center and planner for case investigations..."
Instructions = "You are a forensic expert on Windows Operating systems...
                Primary responsibility: interpret signals, create investigation plans,
                reason over evidence, hypothesize root cause.
                Canonical domains list: registry, filesystem, environment, bootconfig,
                accessibility, searchindexing, shellexplorer, certificates, eventlog,
                applocker, windowsupdate, pnpdevices, hyperv, audio, printers,
                grouppolicy, firewall, localaccounts, rdp, services, scheduledtasks,
                power, network, dcom, wmi, drivers, processes, performance,
                installedapps, browserconfig, fonts, notifications, vpn, wireless,
                proxy, sensors, battery, display, credentials, UAC, defender, bitlocker"
```

#### `TheManager` (Magnetic Manager)
```csharp
Name = "TheManager"
Description = "Magnetic Orchestration Manager agent is responsible for executing the tasks given to it by The Core."
Instructions = "You are the SentinelCore Manager, a magnetic orchestration agent.
                Receive structured investigation plan from Core agent.
                Execute plan by dispatching Domain Agents and dynamic composite agents.
                Rules: Don't reason beyond plan. Delegate each step to correct Domain Agent.
                For cross-domain steps, invoke 'dynamic_agent' tool.
                Collect structured results, synthesize into single response to Core.
                Don't own case lifecycle. Don't write evidence. Only return findings."
```

#### `TheAnalyst` / `TheDomainInvestigator` (Domain Agents)
```csharp
Name = "TheAnalyst"
Description = "Small focused agent with limited scope that gathers evidence from a specific Windows configuration surface..."
Instructions = "You are a special software forensics investigator. Use tools to gather information.
                Given specific area to gather evidence. Must use tools to complete task.
                Response: clear concise natural language, no code blocks.
                Do not reason beyond task. Orchestration Manager may provide additional instructions."
```

#### `TheWorker` (Magnetic Workers)
```csharp
Name = "TheWorker"
Description = "Pragmatic, tireless doer who turns specifications into working code without overthinking."
Instructions = "You are a no-nonsense worker who gets things done. Don't overthink — take
                specification at face value and produce clean, functional code. Favor straightforward
                implementations. When ambiguity, make reasonable assumption, document it, keep moving.
                Shipping beats perfection. Tone: direct, practical, unpretentious."
```

#### `TheCritic` (Magnetic Critic)
```csharp
Name = "TheCritic"
Description = "High value worker that keeps other models in check and forces them to challenge their assumptions."
Instructions = "You are a quality control specialist. Verify other workers take correct approach.
                Critical thinker, challenge assumptions. Skeptic, not easily swayed. Perfectionist.
                Problem solver. Team player. Leader when necessary."
```

#### `TheAggregator` (Magnetic Aggregator)
```csharp
Name = "TheAggregator"
Description = "High value worker that takes the results of other workers and aggregates them into a single result."
Instructions = "You are a data aggregator. Take results of other workers, aggregate into single result.
                Skeptic, not easily swayed. Perfectionist. Problem solver. Team player. Leader."
```

### PersonaRegistry Lookup

```csharp
public static AgentPersona Get(PersonaType type)
    => _personas.TryGetValue(type, out var persona)
        ? persona
        : throw new ArgumentOutOfRangeException(nameof(type), $"Persona not registered: {type}");
```

---

## 5. AgentPersona — Persona Contract

**File:** `Personas/PersonaFactory.cs` (record)
**Interface:** `IAgentPersona` (in `SentinelCore.Contracts.Abstractions`)

```csharp
public record AgentPersona : IAgentPersona
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public interface IAgentPersona
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
}
```

**Usage:** `AgentSpec.Persona` carries the persona through the construction pipeline to `AgentBuilder` which applies it as system instructions to the `ChatClientAgent`.

---

## 6. Two-Stage Construction Pipeline

### Stage 1: Spec Assembly (`IAgentSpecBuilder`)

```
AgentRole
    │
    ▼
AgentSpecBuilder.BuildAgentSpec(role)
    │
    ├─▶ AgentName (role-based)
    ├─▶ Persona (PersonaRegistry lookup)
    ├─▶ Model (SentinelCoreSettings resolution)
    ├─▶ Tools (ToolRegistry.GetToolsetByRole)
    │
    ▼
AgentSpec (immutable record)
```

### Stage 2: Agent Construction (`IAgentBuilder`)

```
AgentSpec
    │
    ▼
AgentBuilder.Build(spec)
    │
    ├─▶ OllamaApiClient(spec.Model.Endpoint, spec.Model.ModelId)
    ├─▶ LoggingChatClient (trace logging)
    ├─▶ EventPublishingChatClient (routes events by AgentRole)
    ├─▶ ChatClientAgent(spec.AgentName, spec.Persona.Instructions, spec.Tools)
    ├─▶ Role-based middleware (PatternMemoryInjector for Core, etc.)
    │
    ▼
AIAgent (fully configured)
```

**Key Principle:** `AgentSpecBuilder` knows **what** an agent gets; `AgentBuilder` knows **how** to build it. Factories (`CoreAgentFactory`, `DomainAgentFactory`) orchestrate both stages and apply role-specific overrides.

---

## 7. Factory Integration

### CoreAgentFactory (Singleton)

```csharp
public AIAgent Create()
{
    AgentSpec baseSpec = _specBuilder.BuildAgentSpec(AgentRole.Core);

    // Add MCP research tools (require runtime endpoint)
    List<AITool> coreTools = [
        new MicrosoftDocsSearchTool(mcp),
        new MicrosoftDocsFetchTool(mcp),
        new MicrosoftCodeSampleSearchTool(mcp)
    ];

    AgentSpec spec = baseSpec with { Tools = [.. baseSpec.Tools, .. coreTools] };
    return _agentBuilder.Build(spec);
}
```

### DomainAgentFactory (Transient)

```csharp
public AIAgent CreateAgent(string domain, string description)
{
    AgentSpec baseSpec = _specBuilder.BuildAgentSpec(AgentRole.Domain);

    // Resolve domain-specific tools
    IList<AITool>? domainTools = ToolRegistry.GetToolByDomain(domain);
    IReadOnlyList<AITool> tools = domainTools is not null ? domainTools.ToList() : [];

    AgentSpec spec = baseSpec with
    {
        AgentName = $"{domain}_agent",
        Tools = tools,
        Persona = string.IsNullOrWhiteSpace(description)
            ? baseSpec.Persona
            : baseSpec.Persona with { Description = description }
    };

    return _agentBuilder.Build(spec);
}
```

### MagneticCoopOrchestration (Direct Builder Usage)

```csharp
var manager = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Manager));
var worker1 = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Worker));
var worker2 = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Worker));
var critic = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.General));
var aggregator = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Aggregator));
```

---

## 8. Configuration Integration

### SentinelCoreSettings → Model Resolution

```csharp
public sealed class SentinelCoreSettings
{
    public ModelSettings? CoreModel { get; set; }      // AgentRole.Core
    public ModelSettings? ManagerModel { get; set; }   // AgentRole.Manager
    public ModelSettings? DomainModel { get; set; }    // AgentRole.Domain
    public ModelSettings? DefaultModel { get; set; }   // Worker, General, Aggregator
    // ...
}
```

### ModelSettings Structure

```csharp
public sealed class ModelSettings
{
    public ModelSettings(string endpoint, string modelId,
                         float temperature = 0.1f,
                         int? maxOutputTokens = 4000,
                         int topK = 1, float topP = 1.0f);

    public string Endpoint { get; init; } = "http://127.0.0.1:11434";
    public string ModelId { get; init; } = string.Empty;
    public float Temperature { get; set; } = 0.1f;
    public int TopK { get; set; } = 1;
    public float TopP { get; set; } = 1.0f;
    public int? MaxOutputTokens { get; set; } = 4000;
}
```

---

## 9. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| AgentRole is single source of truth | ✅ | All defaults derived from role |
| AgentSpec is immutable record | ✅ | `sealed record` with init-only props |
| PersonaRegistry has 35 personas | ✅ | All defined in `PersonaFactory.cs` |
| Role → PersonaType mapping explicit | ✅ | In `AgentSpecBuilder.GetDefaultPersonaType` |
| Model resolution has fail-fast fallback | ✅ | Throws if role model + DefaultModel missing |
| Two-stage construction (Spec → Build) | ✅ | `IAgentSpecBuilder` → `IAgentBuilder` |
| Factories orchestrate, don't duplicate logic | ✅ | Use builder + spec builder |
| MCP tools only added at factory level | ✅ | Not in ToolRegistry |

---

## 10. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| `ActiveAgents` dictionary in `AgentSpecBuilder` | `AgentSpecBuilder.cs` | ❌ `NotImplementedException` |
| Dynamic agent creation (composite agents) | `MagneticCoopOrchestration` | ⚠️ Referenced as 'dynamic_agent' tool |
| Persona customization via configuration | `SentinelCoreSettings` | ❌ Not supported |
| Agent spec validation | `AgentSpecBuilder` | ⚠️ Basic null checks only |
| Toolset for Worker/General/Aggregator roles | `ToolRegistry.RoleToolNames` | ⚠️ Minimal defaults |

---

## 11. Related Documentation

| Document | Description |
|----------|-------------|
| `OrchestrationComponent.md` | Agent construction pipeline, orchestration strategies |
| `DomainAgentSurfaces.md` | Tool registry, domain agent factory, 30+ tools |
| `ToolingComponent.md` | Complete tool registry API |
| `ContractsComponent.md` | `AgentRole`, `AgentSpec`, `ModelSettings`, `SentinelCoreSettings` |
| `MemoryLayerComponent.md` | Pattern memory integration with Core agent |
| `SafetyRailsComponent.md` | Safety middleware applied in AgentBuilder |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
