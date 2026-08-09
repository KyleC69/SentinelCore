---
title: "Orchestration Component"
status: Active
component: Orchestration
last_updated: 2026-07-19
version: v1.0
---

# Orchestration Component

## Overview

The **Orchestration Component** (`SentinelCore.Orchestrations`) is the middle layer of the SentinelCore three-layer architecture. It is responsible for **agent construction, orchestration strategy selection, workflow execution, and event publishing**. It depends **only** on `SentinelCore.Contracts` (zero dependencies on `SentinelCore.CaseFlowEngine` or host projects).

**Architectural Position:**
```
SentinelCore.Contracts (abstractions, DTOs, events)
        ↑ depends on
SentinelCore.Orchestrations (agents, orchestrators, workflows, DI)
        ↑ depends on
SentinelCore.CaseFlowEngine (case lifecycle, persistence)
```

---

## 1. Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Agent Construction** | Two-stage pipeline: `IAgentSpecBuilder` → `AgentSpec` → `IAgentBuilder` → `AIAgent` with role-based defaults, logging, events, middleware |
| **Orchestration Strategy Selection** | `IOrchestrationFactory` selects 1 of 7 `OrchestrationType` strategies at runtime via DI |
| **Workflow Execution** | `ISentinelWorkflowExecution` provides universal streaming execution with event capture, logging, and structured results |
| **Event Publishing** | `EventPublishingChatClient` middleware routes agent events to 7 typed channels via `ISentinelCoreEvents` |
| **Persona Management** | `PersonaRegistry` provides 35 predefined personas mapped to `AgentRole` |
| **Tool Resolution** | `ToolRegistry` statically resolves 30+ domain tools by domain name or role |

---

## 2. Core Abstractions (Contracts Layer)

### 2.1 `ISentinelWorkflow` (Unified Orchestration Interface)

```csharp
public interface ISentinelWorkflow
{
    Task<Workflow> GetBuiltWorkflowAsync(CancellationToken cancellationToken = default);
}
```

**Purpose:** Single interface implemented by all orchestration strategies. Returns a built `Microsoft.Agents.AI.Workflows.Workflow` ready for execution.

**Implementations:**
- `MagneticCoopOrchestration` — Magnetic workflow (Manager + 2 Workers + Critic + Aggregator)
- `TheCoreWorkflow` — Core agent + switch routing to Investigation/Analysis/Safety/DirectAnswer executors
- `MagneticOrchestration` — Legacy magnetic interface (`IMagneticOrchestration`)
- `SequentialOrchestration` — Stub (NotImplementedException)
- `GroupTurnBasedOrchestration` — Stub (NotImplementedException)
- `GroupConcurrentOrchestration` — Concurrent group (Planner + 2 Coop Agents + Critic)
- `SingleAgent` — Single Core agent execution

### 2.2 `IOrchestrationControl` (Host Entry Point)

```csharp
public interface IOrchestrationControl
{
    Task InitializeOrchestrationAsync(ChatMessage promptSignal, CancellationToken token);
}
```

**Purpose:** Single entry point for host applications. Resolves the configured `OrchestrationType` via `IOrchestrationFactory`, builds the workflow, and delegates execution to `ISentinelWorkflowExecution`.

**Implementation:** `OrchestrationControl`

### 2.3 `OrchestrationType` (Strategy Enum)

```csharp
public enum OrchestrationType
{
    TheCore,          // TheCoreWorkflow: Core agent + switch routing
    Magnetic,         // MagneticCoopOrchestration: Manager + 2 Workers + Critic + Aggregator
    GroupTurnBased,   // Stub
    GroupConcurrent,  // GroupConcurrentOrchestration: Planner + 2 Coop + Critic (parallel)
    Sequential,       // Stub
    Investigative,    // Maps to TheCoreWorkflow (commented in factory)
    SingleAgent       // SingleAgent: Direct Core agent execution
}
```

**Configuration:** `SentinelCoreSettings.OrchestrationType` (default: `Magnetic`)

---

## 3. Agent Construction Pipeline

### 3.1 Two-Stage Construction

```
IAgentSpecBuilder.BuildAgentSpec(AgentRole, Persona)
    → AgentSpec (immutable record: Role, AgentName, Persona, Tools, Model)
    → IAgentBuilder.Build(AgentSpec)
    → AIAgent (with LoggingChatClient → EventPublishingChatClient → middleware)
```

### 3.2 `IAgentSpecBuilder` / `AgentSpecBuilder`

**Responsibilities:**
- Role-based model resolution (`ResolveModel(AgentRole)` → `ModelSettings` from `SentinelCoreSettings`)
- Role-based default persona (`GetDefaultPersonaType(AgentRole)` → `PersonaType`)
- Role-based agent naming (`GetAgentName(AgentRole)` → e.g., "SentinelCore-Core", "SentinelCore-Manager")

**AgentRole Enum (6 roles):**
```csharp
public enum AgentRole
{
    Core,       // The Core agent: planning, classification, synthesis
    Manager,    // Magnetic orchestration manager
    Domain,     // Domain-specific agents (resolved via ToolRegistry)
    Worker,     // Magnetic workflow workers
    General,    // General-purpose (Critic, Planner, etc.)
    Aggregator  // Output aggregation agent
}
```

### 3.3 `IAgentBuilder` / `AgentBuilder` (Single Shared Pipeline)

**Pipeline (applied to ALL agents):**
1. `OllamaApiClient` (from `ModelSettings`)
2. `LoggingChatClient` (structured logging)
3. `EventPublishingChatClient` (routes events by `AgentRole` to `ISentinelCoreEvents`)
4. `ChatClientAgent` (MAF agent wrapper)
5. **Role-based middleware:**
   - `Core` → `PatternMemoryInjector` (injects pattern memory context)
   - `Manager` → (no additional middleware)
   - `Domain` → (no additional middleware; tools injected at spec level)
   - `Worker` / `General` / `Aggregator` → (no additional middleware)

### 3.4 Agent Factories (Role-Specific Spec Customization)

| Factory | Role | Customizations |
|---------|------|----------------|
| `CoreAgentFactory` | `Core` | Adds MCP tools: `MicrosoftDocsSearchTool`, `MicrosoftDocsFetchTool`, `MicrosoftCodeSampleSearchTool` |
| `DomainAgentFactory` | `Domain` | Overrides `AgentName`, `Tools` (via `ToolRegistry.GetToolByDomain(domain)`), `Persona.Description` |

---

## 4. Orchestration Strategies (Implementations)

### 4.1 `MagneticCoopOrchestration` (OrchestrationType.Magnetic)

**Workflow:** `MagenticWorkflowBuilder` with:
- **Manager** (`AgentRole.Manager`, `PersonaType.TheManager`)
- **Worker 1** (`AgentRole.Worker`, `PersonaType.TheWorker`)
- **Worker 2** (`AgentRole.Worker`, `PersonaType.TheWorker`)
- **Critic** (`AgentRole.General`, `PersonaType.TheCritic`)
- **Aggregator** (`AgentRole.Aggregator`, `PersonaType.TheAggregator`)

**Configuration:**
- `RequirePlanSignoff = false`
- `MaxRounds = 8`
- `MaxResets = 3`
- `MaxStalls = 2`
- Output from Aggregator

**Execution:** Delegates to `SentinelWorkflowExecution.ExecuteAsync()`

### 4.2 `TheCoreWorkflow` (OrchestrationType.TheCore / Investigative)

**Architecture:** Core agent classifies signal → `WorkflowBuilder.AddSwitch` routes to executor:

| NextStep | Executor | Description |
|----------|----------|-------------|
| `Investigate` | `InvestigationExecutor` | Magentic sub-workflow (Manager + 2 Workers + Critic + Aggregator) |
| `RedAlert` / `EscalateToHumanOperator` | `SafetyExecutor` | Safety agent (`PersonaType.TheEvaluator`) |
| `CanAnswerDirectly` / `PatternMatch` / `IsNoise` / `MoreInformationRequired` | `DirectAnswerExecutor` | Direct answer from Core |

**Shared Message Type:** `CaseHypothesis` flows between steps.

**Agents Built:**
- Core (classification) → `PersonaType.TheCore`
- Investigation Manager → `PersonaType.TheDecisionMaker`
- Investigation Workers (2) → `PersonaType.TheCollaborator`
- Investigation Critic → `PersonaType.TheCritic`
- Analysis Manager → `PersonaType.TheDecisionMaker`
- Analysis Workers (2) → `PersonaType.TheCollaborator`
- Analysis Critic → `PersonaType.TheCritic`
- Aggregator → `PersonaType.TheAggregator`
- Safety → `PersonaType.TheEvaluator`
- Core Final → `PersonaType.TheCore`

**Sub-workflows:**
- Investigation: `MagenticWorkflowBuilder` (MaxRounds=6, MaxResets=2, MaxStalls=2)
- Analysis: `AgentWorkflowBuilder.CreateConcurrentBuilderWith` (4 participants)

### 4.3 `GroupConcurrentOrchestration` (OrchestrationType.GroupConcurrent)

**Workflow:** `AgentWorkflowBuilder.CreateConcurrentBuilderWith` with 4 agents:
- **Planner** (`General`, `ConcurrentPlanner`) — MCP research tools
- **CoopAgent_dotnet** (`General`) — .NET expert, MCP tools
- **CoopAgent_Framework** (`General`) — Agent Framework expert, MCP tools
- **Critic** (`General`, `ConcurrentCritic`) — Architecture critic, MCP tools

**Aggregation:** Custom `AggregateAgentOutputs` concatenates all outputs + synthesis message.

### 4.4 `SingleAgent` (OrchestrationType.SingleAgent)

**Workflow:** Direct `AIAgent.RunAsync()` on Core agent (built via `AgentSpecBuilder.BuildAgentSpec(AgentRole.Core)`).

**Returns:** `WorkflowExecutionResult` with single assistant message.

### 4.5 Stubs (Not Implemented)

- `SequentialOrchestration` — `NotImplementedException`
- `GroupTurnBasedOrchestration` — `NotImplementedException`
- `MagneticOrchestration` — Legacy `IMagneticOrchestration` interface, stubs `ISentinelWorkflow` methods

---

## 5. Workflow Execution Engine

### 5.1 `ISentinelWorkflowExecution` / `SentinelWorkflowExecution`

**Universal execution engine** — all orchestrators delegate here.

```csharp
public interface ISentinelWorkflowExecution
{
    Task<WorkflowExecutionResult> ExecuteAsync(Workflow workflow, ChatMessage promptSignal, string phaseLabel, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionResult> ExecuteAsync(Workflow workflow, string promptText, string phaseLabel, CancellationToken cancellationToken = default);
    Task<WorkflowExecutionResult> ExecuteAsync(Workflow workflow, ChatMessage promptSignal, CancellationToken cancellationToken = default);
}
```

**Execution Flow:**
1. `InProcessExecution.RunStreamingAsync(workflow, promptSignal)`
2. `run.TrySendMessageAsync(new TurnToken(emitEvents: true))`
3. `await foreach (WorkflowEvent evt in run.WatchStreamAsync())`
4. `ProcessEvent(evt, phaseLabel, eventLog)` → routes to `ISystemReporter` + `ISentinelCoreEvents`
5. Captures `WorkflowOutputEvent` → `finalMessages`
6. Returns `WorkflowExecutionResult`

### 5.2 Event Processing (`ProcessEvent`)

| WorkflowEvent Type | SystemReporter | SentinelCoreEvents Channel | EventLog Entry |
|--------------------|----------------|---------------------------|----------------|
| `AgentResponseUpdateEvent` | `ReportInfo` | — | `WorkflowEventType.AgentResponse` |
| `MagenticPlanCreatedEvent` | — | `RaiseOrchestrationEvent` | `WorkflowEventType.MagenticPlanCreated` |
| `MagenticReplannedEvent` | — | `RaiseOrchestrationEvent` | `WorkflowEventType.MagenticReplanned` |
| `MagenticProgressLedgerUpdatedEvent` | `ReportInfo` | — | `WorkflowEventType.MagenticProgress` |
| `WorkflowOutputEvent` | `ReportInfo` | — | `WorkflowEventType.WorkflowOutput` |
| `WorkflowErrorEvent` | `ReportError` | `RaiseOrchestrationEvent` | `WorkflowEventType.Error` |
| `ExecutorFailedEvent` | `ReportError` | — | `WorkflowEventType.ExecutorFailed` |
| *Unknown* | — | — | `WorkflowEventType.Unknown` |

### 5.3 Result Types

```csharp
public sealed class WorkflowExecutionResult
{
    public List<ChatMessage>? OutputMessages { get; }
    public IReadOnlyList<WorkflowEventEntry> EventLog { get; }
    public bool HasOutput => OutputMessages?.Count > 0;
    public string? LastAssistantMessage { get; }
}

public sealed class WorkflowEventEntry
{
    public WorkflowEventType EventType { get; }
    public string Source { get; }
    public string Message { get; }
    public DateTime Timestamp { get; } = DateTime.Now;
}

public enum WorkflowEventType
{
    AgentResponse,
    MagenticPlanCreated,
    MagenticReplanned,
    MagenticProgress,
    WorkflowOutput,
    Error,
    ExecutorFailed,
    Unknown
}
```

---

## 6. Factory & DI Integration

### 6.1 `IOrchestrationFactory` / `OrchestrationFactory`

```csharp
public interface IOrchestrationFactory
{
    Workflow CreateOrchestrationInstance(OrchestrationType orchestrationType);
    void Run();
}
```

**Implementation:** Resolves orchestration services from DI container and calls `BuildWorkflow()`:

```csharp
return orchestrationType switch
{
    OrchestrationType.TheCore => _sp.GetRequiredService<TheCoreWorkflow>().BuildWorkflow(),
    OrchestrationType.Magnetic => _sp.GetRequiredService<MagneticCoopOrchestration>().BuildWorkflow(),
    // GroupTurnBased, GroupConcurrent, Sequential, Investigative, SingleAgent commented out
    _ => throw new NotSupportedException($"Orchestration type '{orchestrationType}' is not supported.")
};
```

**Registered in DI:** `services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>()`

### 6.2 `OrchestrationControl` (Host Entry Point)

```csharp
public class OrchestrationControl : IOrchestrationControl
{
    public OrchestrationControl(
        IOrchestrationFactory orchestrationFactory,
        IOptions<SentinelCoreSettings> settings,
        ISentinelCoreEvents events,
        ISystemReporter systemReporter,
        ISentinelWorkflowExecution workflowExecution)
    {
        _orchestration = orchestrationFactory.CreateOrchestrationInstance(settings.Value.OrchestrationType);
        // ... validation ...
    }

    public async Task InitializeOrchestrationAsync(ChatMessage promptSignal, CancellationToken token)
    {
        _events.RaiseOrchestrationEvent(new OrchestrationActivityArgs("Starting orchestration", _orchestration.GetType().Name));
        var result = await _workflowExecution.ExecuteAsync(_orchestration, promptSignal, _orchestration.GetType().Name, token);
        _events.RaiseOrchestrationEvent(new OrchestrationActivityArgs(
            result.HasOutput ? "Orchestration completed successfully." : "Orchestration completed with no output.",
            _orchestration.GetType().Name));
    }
}
```

### 6.3 DI Registration (`SentinelCoreServiceExtensions`)

```csharp
// Core services
services.AddSingleton<IAgentBuilder, AgentBuilder>();
services.AddSingleton<IAgentSpecBuilder, AgentSpecBuilder>();
services.AddSingleton<ISentinelWorkflowExecution, SentinelWorkflowExecution>();
services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>();
services.AddSingleton<IOrchestrationControl, OrchestrationControl>();

// Orchestrators
services.AddSingleton<MagneticCoopOrchestration>();
services.AddSingleton<TheCoreWorkflow>();
services.AddSingleton<GroupConcurrentOrchestration>();
services.AddSingleton<SingleAgent>();
services.AddSingleton<SequentialOrchestration>();
services.AddSingleton<GroupTurnBasedOrchestration>();
services.AddSingleton<MagneticOrchestration>();

// Agent factories
services.AddSingleton<ICoreAgentFactory, CoreAgentFactory>();
services.AddSingleton<IDomainAgentFactory, DomainAgentFactory>();

// Personas & Tools
services.AddSingleton<PersonaRegistry>();
services.AddSingleton<ToolRegistry>();
```

---

## 7. Event Publishing Middleware

### 7.1 `EventPublishingChatClient` (Middleware in AgentBuilder Pipeline)

**Purpose:** Intercepts agent streaming updates and routes to typed `ISentinelCoreEvents` channels by `AgentRole`.

**Routing Map:**
| AgentRole | Event Channel |
|-----------|---------------|
| `Core` | `RaiseTheCoreActivity(CoreActivityArgs)` |
| `Manager` | `RaiseOrchestrationEvent(OrchestrationActivityArgs)` |
| `Domain` | `RaiseDomainAgentActivity(DomainAgentActivityArgs)` |
| `Worker` | `RaiseWorkerActivity(WorkerActivityArgs)` |
| `General` | `RaiseGeneralAgentActivity(GeneralAgentActivityArgs)` |
| `Aggregator` | `RaiseAggregatorActivity(AggregatorActivityArgs)` |

**Implementation:** Wraps `IChatClient`, overrides `GetStreamingResponseAsync`, yields updates while publishing events.

---

## 8. Persona Registry

### 8.1 `PersonaRegistry` / `PersonaType` (35 Personas)

**Registry:** `Dictionary<PersonaType, AgentPersona>` with `Get(PersonaType)` accessor.

**Key Personas by Role:**

| PersonaType | Role | Used By |
|-------------|------|---------|
| `TheCore` | Core | CoreAgentFactory, TheCoreWorkflow |
| `TheManager` | Manager | MagneticCoopOrchestration |
| `TheDecisionMaker` | Manager | TheCoreWorkflow (Investigation/Analysis managers) |
| `TheWorker` | Worker | MagneticCoopOrchestration |
| `TheCollaborator` | Worker | TheCoreWorkflow (Investigation/Analysis workers) |
| `TheCritic` | General | MagneticCoopOrchestration, TheCoreWorkflow, GroupConcurrentOrchestration |
| `TheAggregator` | Aggregator | MagneticCoopOrchestration, TheCoreWorkflow |
| `TheEvaluator` | General | TheCoreWorkflow (SafetyExecutor) |
| `ThePlanner` | General | GroupConcurrentOrchestration (commented) |
| `ConcurrentPlanner` | General | GroupConcurrentOrchestration |
| `ConcurrentCritic` | General | GroupConcurrentOrchestration |

---

## 9. Tool Registry

### 9.1 `ToolRegistry` (Static Resolution)

**Methods:**
- `GetToolByDomain(string domain)` → `List<AITool>` (30+ domain-specific tools)
- `GetToolsByNames(IEnumerable<string> toolNames)` → `List<AITool>`
- `GetToolsetByRole(AgentRole role)` → `List<AITool>` (role-based toolsets)
- `GetAllTools()` → `List<AITool>`

**Domain → Tools Mapping (`DomainToolNames` dictionary):**
- `windows` → `GetWindowsVersionTool`, `GetInstalledSoftwareTool`, `GetRunningProcessesTool`, `GetSystemInfoTool`, `GetEventLogsTool`, `GetRegistryValueTool`, `GetScheduledTasksTool`, `GetServicesTool`, `GetDiskSpaceTool`, `GetNetworkConfigurationTool`, `GetEnvironmentVariablesTool`, `GetInstalledUpdatesTool`, `GetStartupProgramsTool`, `GetUserAccountsTool`, `GetWindowsFeaturesTool`, `GetPowerShellExecutionPolicyTool`, `GetWindowsDefenderStatusTool`, `GetBitLockerStatusTool`, `GetFirewallRulesTool`, `GetWindowsTimeServiceStatusTool`
- `dotnet` → `GetDotNetSdkVersionsTool`, `GetDotNetRuntimeVersionsTool`, `GetNuGetPackageVersionsTool`, `GetProjectDependenciesTool`, `GetProjectFrameworkTool`, `GetProjectReferencesTool`, `GetProjectPackageReferencesTool`, `GetProjectSdkTool`, `GetProjectTargetFrameworksTool`, `GetProjectPropertyTool`, `GetProjectItemsTool`, `GetProjectBuildOutputTool`, `GetProjectAnalyzersTool`, `GetProjectNuGetConfigTool`, `GetProjectAssemblyAttributesTool`, `GetProjectLangVersionTool`, `GetProjectNullableContextTool`, `GetProjectImplicitUsingsTool`, `GetProjectTreatWarningsAsErrorsTool`, `GetProjectOutputTypeTool`
- `agentframework` → `GetAgentFrameworkVersionTool`, `GetAgentFrameworkPackagesTool`, `GetAgentFrameworkSamplesTool`, `GetAgentFrameworkDocumentationTool`, `GetAgentFrameworkArchitectureTool`, `GetAgentFrameworkBestPracticesTool`, `GetAgentFrameworkSamplesByScenarioTool`, `GetAgentFrameworkNuGetPackagesTool`, `GetAgentFrameworkSourceCodeTool`, `GetAgentFrameworkReleaseNotesTool`
- `mcp` → `MicrosoftDocsSearchTool`, `MicrosoftDocsFetchTool`, `MicrosoftCodeSampleSearchTool` (MCP-based, used by Core agent)

**Role → Toolset Mapping (`RoleToolNames`):**
- `Core` → `mcp` tools
- `Manager` → (empty)
- `Domain` → Resolved dynamically via `GetToolByDomain(domain)`
- `Worker` → (empty)
- `General` → (empty)
- `Aggregator` → (empty)

---

## 10. Configuration

### 10.1 `SentinelCoreSettings` (from Contracts)

```csharp
public class SentinelCoreSettings
{
    public OrchestrationType OrchestrationType { get; set; } = OrchestrationType.Magnetic;
    public Dictionary<AgentRole, ModelSettings> ModelSettings { get; set; } = new();
    public string DefaultModel { get; set; } = "phi4-mini:latest";
    public string SqlConnectionString { get; set; } = "";
    public bool TraceEnabled { get; set; } = false;
    public LogLevel TraceLogLevel { get; set; } = LogLevel.Trace;
}
```

**ModelSettings per Role:**
```csharp
public class ModelSettings
{
    public string ModelId { get; set; } = "phi4-mini:latest";
    public float Temperature { get; set; } = 0.1f;
    public int MaxTokens { get; set; } = 4096;
    public string Endpoint { get; set; } = "http://localhost:11434";
}
```

---

## 11. Interaction Diagram (Conceptual)

```
Host (SentinelCoreHost)
    │
    ▼
IOrchestrationControl.InitializeOrchestrationAsync(ChatMessage)
    │
    ▼
OrchestrationControl
    │
    ├─► IOrchestrationFactory.CreateOrchestrationInstance(OrchestrationType)
    │       │
    │       ├─► TheCoreWorkflow.BuildWorkflow()
    │       ├─► MagneticCoopOrchestration.BuildMagWorkflow()
    │       ├─► GroupConcurrentOrchestration.BuildWorkflow()
    │       └─► SingleAgent (direct agent)
    │
    ▼
ISentinelWorkflowExecution.ExecuteAsync(Workflow, ChatMessage, phaseLabel)
    │
    ├─► InProcessExecution.RunStreamingAsync()
    │
    ├─► WatchStreamAsync() → ProcessEvent() → ISentinelCoreEvents + ISystemReporter
    │
    ▼
WorkflowExecutionResult (OutputMessages + EventLog)
```

---

## 12. Contracts & Invariants

| Invariant | Enforcement |
|-----------|-------------|
| **Single orchestration interface** | All orchestrators implement `ISentinelWorkflow` |
| **Unified execution** | All orchestrators delegate to `ISentinelWorkflowExecution` |
| **Factory selects strategy** | `IOrchestrationFactory` is sole creator of `Workflow` instances |
| **Agent pipeline is shared** | `AgentBuilder` is singleton; all agents use same middleware chain |
| **Events routed by role** | `EventPublishingChatClient` switches on `AgentRole` |
| **No direct CFE dependency** | Orchestrations reference only `SentinelCore.Contracts` abstractions |
| **Persona registry is authoritative** | `PersonaRegistry.Get(PersonaType)` is single source of truth |
| **Tool registry is static** | `ToolRegistry` methods are static; no DI required for tool resolution |

---

## 13. Implementation Status

| OrchestrationType | Implementation | Status |
|-------------------|----------------|--------|
| `TheCore` | `TheCoreWorkflow` | **Complete** — Complex switch-based routing with sub-workflows |
| `Magnetic` | `MagneticCoopOrchestration` | **Complete** — Magentic workflow with 5 agents |
| `GroupConcurrent` | `GroupConcurrentOrchestration` | **Complete** — Concurrent group with custom aggregation |
| `SingleAgent` | `SingleAgent` | **Complete** — Direct Core agent execution |
| `GroupTurnBased` | `GroupTurnBasedOrchestration` | **Stub** — `NotImplementedException` |
| `Sequential` | `SequentialOrchestration` | **Stub** — `NotImplementedException` |
| `Investigative` | (maps to TheCore) | **Commented in factory** |

---

## 14. Related Components

| Component | Relationship |
|-----------|--------------|
| `SentinelCore.Contracts` | Defines `ISentinelWorkflow`, `IOrchestrationControl`, `OrchestrationType`, `AgentRole`, `ISentinelCoreEvents`, `SentinelCoreSettings` |
| `SentinelCore.CaseFlowEngine` | Consumed by host; orchestration receives `ChatMessage` from CFE via host |
| `SentinelCoreHost` | Configures `OrchestrationType` in `SentinelCoreSettings`, calls `IOrchestrationControl` |
| `PersonaRegistry` | Provides 35 personas for agent construction |
| `ToolRegistry` | Provides 30+ domain tools for agent toolbelts |
| `SentinelWorkflowExecution` | Universal execution engine for all orchestrators |

---

## 15. TODOs / Known Gaps

| Item | Description |
|------|-------------|
| `GroupTurnBasedOrchestration` | Implement turn-based group workflow |
| `SequentialOrchestration` | Implement sequential workflow |
| `Investigative` orchestration | Uncomment and wire in factory (currently maps to TheCore) |
| `IOrchestrator` interface | `OrchestrationControl` casts to `IOrchestrator` but interface doesn't exist in codebase |
| `TheCoreOrchestration` (legacy) | Legacy class implementing `ISentinelWorkflow` + `IMagneticOrchestration` — not wired in factory |
| `MagneticOrchestration` (legacy) | Legacy `IMagneticOrchestration` implementation — not wired in factory |
| ToolRegistry completion | `GetAllTools()` method incomplete in source (cut off mid-method) |
| DI validation | `SentinelCoreBuilder` validates module dependency chains — verify orchestration registration |

---

## 16. File Reference

| File | Purpose |
|------|---------|
| `Abstractions/ISentinelWorkflow.cs` | Unified orchestration interface |
| `Abstractions/IOrchestrationControl.cs` | Host entry point interface |
| `Application/OrchestrationControl.cs` | Host entry point implementation |
| `Application/OrchestrationFactory.cs` | Strategy factory (OrchestrationType → Workflow) |
| `Application/SentinelWorkflowExecution.cs` | Universal workflow execution engine |
| `Application/ToolRegistry.cs` | Static tool resolution (30+ tools) |
| `Agents/AgentSpecBuilder.cs` | Role-based AgentSpec construction |
| `Agents/AgentBuilder.cs` | Shared agent construction pipeline |
| `Agents/Core/CoreAgentFactory.cs` | Core agent factory (+ MCP tools) |
| `Agents/Domain/DomainAgentFactory.cs` | Domain agent factory (domain tools) |
| `Agents/Middleware/EventPublishingChatClient.cs` | Event routing middleware |
| `Agents/Middleware/PatternMemoryInjector.cs` | Pattern memory context provider |
| `Orchestrators/MagneticCoopOrchestration.cs` | Magnetic workflow (5 agents) |
| `Orchestrators/TheCoreWorkflow.cs` | Core + switch routing (11 agents, 2 sub-workflows) |
| `Orchestrators/GroupConcurrentOrchestration.cs` | Concurrent group (4 agents) |
| `Orchestrators/SingleAgent.cs` | Single Core agent |
| `Orchestrators/SequentialOrchestration.cs` | Stub |
| `Orchestrators/GroupTurnBasedOrchestration.cs` | Stub |
| `Orchestrators/MagneticOrchestration.cs` | Legacy magnetic interface |
| `Personas/PersonaFactory.cs` | 35-persona registry |
| `Infrastructure/DI/SentinelCoreServiceExtensions.cs` | DI registration |
| `Infrastructure/DI/SentinelCoreBuilder.cs` | DI builder with validation |
