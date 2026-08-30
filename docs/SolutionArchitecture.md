# SentinelCore Solution Architecture

## Overview

SentinelCore is a multi-agent investigation platform that learns from every case to accelerate future resolutions. The system adapts to the user's environment, using local or cloud models without special configuration. It distinguishes itself through **pattern memory** and **magnetic orchestration** — dividing responsibilities among agents to avoid overloading any single agent.

## Solution Structure

The solution is organized into a clear, layered architecture with strict dependency rules to maintain separation of concerns and prevent architectural drift.

### Solution Folders


Solution-Root/
├── docs/                  # Documentation (this folder)
├── projects/              # Source code projects
│   ├── SentinelCore.Contracts/        # Shared abstractions, DTOs, settings, events
│   ├── SentinelCore.CaseFlowEngine/   # Case lifecycle, persistence, pattern memory
│   ├── SentinelCore.Orchestrations/   # Agents, workflows, tools, safety engine, DI
│   ├── SentinelCore.UI/               # WPF host application (composition root)
│   └── SentinelCoreAdmin/             # Legacy admin host (being deprecated)
└── SentinelCore.slnx                 # Solution file


### Projects and Their Responsibilities

| Project | Role | Dependencies |
| --------- | ------ | -------------- |
| `SentinelCore.Contracts` | Shared abstractions, DTOs, settings, events — zero project dependencies | None (only NuGet packages) |
| `SentinelCore.CaseFlowEngine` | Case lifecycle, persistence, pattern memory | `SentinelCore.Contracts` |
| `SentinelCore.Orchestrations` | Agents, workflows, tools, safety engine, DI wiring | `SentinelCore.Contracts`, `SentinelCore.CaseFlowEngine` |
| `SentinelCore.UI` | WPF host application — composition root, DI wiring, chat UI | All three core projects |
| `SentinelCoreAdmin` | Legacy admin host (being deprecated) | All three core projects |

### Dependency Graph

```
┌──────────────────────┐
│  SentinelCore.UI      │  ← WPF host (composition root)
└──────┬───────┬───────┘
       │       │
       ▼       ▼
┌──────────────┐  ┌──────────────────────┐
│  Contracts   │◀─┤  Orchestrations       │
└──────┬───────┘  └──────┬───────────────┘
       │                  │
       ▼                  ▼
┌──────────────────┐  ┌──────────────────┐
│  CaseFlowEngine  │◀─┤  Orchestrations  │
└──────────────────┘  │  (also depends   │
                       │  on Contracts)   │
                       └──────────────────┘
```

> **Note:** `SentinelCore.Orchestrations` references both `SentinelCore.Contracts` and `SentinelCore.CaseFlowEngine`. This allows the workflow layer to access case lifecycle operations directly (e.g., `NewCaseExecutor` calls `ICaseFlowEngine.CreateCaseAsync`).

### Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  SentinelCore.UI                                            │
│  WPF application — composition root, bootstraps DI, chat UI │
├─────────────────────────────────────────────────────────────┤
│  SentinelCore.Orchestrations / Infrastructure / DI          │
│  Wiring layer — registers all services                       │
├─────────────────────────────────────────────────────────────┤
│  Workflows                                                   │
│  (TheCoreWorkflow, CustomGroupWorkflow;                      │
│   selected via IOrchestrationFactory)                        │
├─────────────────────────────────────────────────────────────┤
│  Agents & Safety                                             │
│  (AgentProfile, SentinelAgentFactory, SafetyEngineAgent,    │
│   PatternMemoryInjector, EventPublishingChatClient,          │
│   ModelNoiseSafety)                                          │
├─────────────────────────────────────────────────────────────┤
│  Executors                                                   │
│  (TheCoreExec, InvestigationExecutor, SafetyExecutor,         │
│   NewCaseExecutor, AggregationExecutor, PatternCheck, etc.) │
├─────────────────────────────────────────────────────────────┤
│  CaseFlow                                                    │
│  (ICaseFlowEngine, CaseFlowEngine)                           │
├─────────────────────────────────────────────────────────────┤
│  Contracts / Abstractions                                    │
│  (ISystemReporter, ICaseFlowEngine, IEvidenceStore,          │
│   ISignalRepository, IPatternMemoryStore, ICaseGenerator,    │
│   ISentinelCoreEvents, SentinelCoreSettings,                 │
│   CaseStatus, Signal, Case, Evidence, etc.)                  │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules

| Rule | Description |
| ------ | ------------- |
| **Contracts is zero-dependency** | `SentinelCore.Contracts` contains only pure DTOs, settings, abstractions, and events. No project references — only NuGet packages. |
| **CaseFlowEngine depends on Contracts only** | `SentinelCore.CaseFlowEngine` references `SentinelCore.Contracts` for abstractions and DTOs. |
| **Orchestrations depends on Contracts + CaseFlowEngine** | `SentinelCore.Orchestrations` references both projects. It needs `ICaseFlowEngine` for case operations in workflow executors. |
| **Orchestration abstractions live in Orchestrations** | `IOrchestration`, `IOrchestrationFactory`, `IAgentPersona`, `IAgentProfileBuilder`, `ISentinelAgentFactory`, `ICaseGenerator` are in `SentinelCore.Orchestrations.Abstractions`. |
| **Safety types live in Orchestrations** | `ISafetyRule`, `SafetyEngineAgent`, `SafetyEvaluationContext`, `SafetyRuleResult`, `SafetyAction`, `SafetySeverity` are in `SentinelCore.Orchestrations.SafetyEngine`. |

### Forbidden Dependency Directions

- Contracts → Orchestrations ❌ (Contracts must be pure)
- Contracts → CaseFlowEngine ❌ (Contracts must be pure)
- CaseFlowEngine → Orchestrations ❌ (CaseFlow must not depend on orchestration)

### Namespace Convention

All namespaces use the `SentinelCore.*` root. Namespaces **must** match the folder path relative to the project root.

Key namespace mappings:

| Project | Namespaces |
| --------- | ----------- |
| Contracts | `SentinelCore.Abstractions`, `SentinelCore.CaseFlow`, `SentinelCore.CaseEngine`, `SentinelCore.Contracts`, `SentinelCore.Events`, `SentinelCore.DependencyInjection` |
| CaseFlowEngine | `SentinelCore.CaseFlowEngine.CaseFlow`, `SentinelCore.CaseFlowEngine.Infrastructure.Persistence`, `SentinelCore.CaseFlowEngine.Infrastructure.DependencyInjection` |
| Orchestrations | `SentinelCore.Agents`, `SentinelCore.Application`, `SentinelCore.Orchestrations`, `SentinelCore.SafetyEngine`, `SentinelCore.Tools`, `SentinelCore.Workflows`, `SentinelCore.Workflows.Executors`, `SentinelCore.Personas`, `SentinelCore.Infrastructure.DependencyInjection` |
| Host (UI) | `SentinelCore.UI`, `SentinelCore.UI.ViewModels`, `SentinelCore.UI.Views`, `SentinelCore.UI.Services`, `SentinelCore.UI.Converters`, `SentinelCore.UI.Models` |

### Key Architectural Patterns

1. **Pattern Memory and Learning** — The system stores vector embeddings of past signals and resolutions, enabling case-based reasoning for new investigations.
2. **Workflow-Based Orchestration** — Uses `Microsoft.Agents.AI.Workflows` for composing multi-step investigation flows with switch-based routing, sub-workflows, and executor composition.
3. **Magnetic Orchestration** — Magentic sub-workflows (Manager + Workers) for evidence collection, integrated as sub-workflows within the main TheCoreWorkflow.
4. **Safety Engine** — Composable rule-based safety pipeline that evaluates agent messages before they reach the model, with Block/Warn/Allow actions.
5. **Layered Architecture** — Strict layering prevents architectural drift and maintains clear separation of concerns.
6. **Dependency Inversion** — Abstractions defined in Contracts, implementations in CaseFlowEngine and Orchestrations.
7. **Agent Middleware Pipeline** — Configurable pipeline flags (logging, events, safety, pattern memory, null safety) applied per agent role via `AgentMiddlewarePipeline`.

### Key Components

- **Contracts** — All shared abstractions, DTOs, enums, events, and configuration. Foundation layer with zero project dependencies.
- **CaseFlowEngine** — Case lifecycle management (CaseFlowEngine), EF Core persistence (SentinelCoreDbContext), and pattern memory storage/search (PatternMemoryStore).
- **Orchestrations** — Agent construction (AgentProfileBuilder → SentinelAgentFactory), workflow composition (TheCoreWorkflow with executors), tool registry (30+ Windows domain tools), safety engine (ISafetyRule implementations), DI wiring (SentinelCoreServiceExtensions).
- **Agents** — Individual AI agents with specific roles (Core, Manager, Domain, Utility), built using `AgentProfileBuilder` and `SentinelAgentFactory`.
- **Safety Engine** — Rule-based safety evaluation pipeline (`ISafetyRule` implementations) applied via `SafetyEngineAgent` as agent middleware.
- **Workflows** — `TheCoreWorkflow` (primary production workflow) with switch-based routing through executors, and `CustomGroupWorkflow` for testing.
- **Events System** — Unified event model via `ISentinelCoreEvents` with `SentinelOutputEventArgs` and `ActivityType` discriminator.
- **Host** — WPF application providing UI, DI composition root, and application lifecycle management.

### Projects in Detail

#### SentinelCore.Contracts

- Contains all shared contracts: DTOs (`Case`, `Signal`, `Evidence`, `InvestigationPlan`, `Resolution`), enums (`CaseStatus`, `OrchestrationType`, `ActivityType`), events (`ISentinelCoreEvents`, `SentinelOutputEventArgs`), abstractions (`ICaseFlowEngine`, `IEvidenceStore`, `ISignalRepository`, `IPatternMemoryStore`, `ISystemReporter`, `ICaseGenerator`), settings (`SentinelCoreSettings`, `ModelProfile`), and DI (`ISentinelCoreBuilder`).
- Has no dependencies on other solution projects (only NuGet packages).
- Serves as the foundation for all other projects.

#### SentinelCore.Orchestrations

- Contains agent profiles (`AgentProfile`, `AgentProfileBuilder`), agent factory (`SentinelAgentFactory`), chat client factory (`SentinelChatClientFactory`), middleware (`EventPublishingChatClient`, `ModelNoiseSafety`, `PatternMemoryInjector`), workflow definitions (`TheCoreWorkflow`, `CustomGroupWorkflow`), executors (TheCoreExec, InvestigationExecutor, SafetyExecutor, NewCaseExecutor, etc.), tool registry (30+ domain tools), safety engine (`SafetyEngineAgent`, `ISafetyRule` implementations), persona registry, and DI wiring.
- Depends on `SentinelCore.Contracts` and `SentinelCore.CaseFlowEngine`.
- Defines orchestration abstractions (`IOrchestration`, `IOrchestrationFactory`, `IAgentPersona`, `IAgentProfileBuilder`, `ISentinelAgentFactory`, `ICaseGenerator`).

#### SentinelCore.CaseFlowEngine

- Implements case lifecycle management (`CaseFlowEngine`), persistence (`SentinelCoreDbContext` with EF Core + SQL Server), and pattern memory (`PatternMemoryStore` with vector similarity search).
- Depends only on `SentinelCore.Contracts`.
- Provides `ICaseFlowEngine` implementation and repository classes (`EvidenceStore`, `SignalRepository`, `PatternMemoryStore`).

#### SentinelCore.UI

- WPF host application serving as the composition root for dependency injection.
- References all three core projects (`Contracts`, `CaseFlowEngine`, `Orchestrations`).
- Contains the chat UI (`CoreChatViewModel`, `CoreChatPage`), dispatcher service, converters, and styles.
- Uses CommunityToolkit.Mvvm for MVVM pattern with source-generated observables and commands.
- Decouples ViewModels from WPF dispatcher via `IDispatcherService` abstraction for testability.

#### SentinelCoreAdmin (Legacy)

- Previous WPF host application with MahApps.Metro shell, Template Studio scaffolding, and navigation framework.
- Being deprecated in favor of `SentinelCore.UI`. Features will migrate incrementally.

### Related Documents

- [ContractsComponent.md](./ContractsComponent.md) - Contracts layer details
- [OrchestrationComponent.md](./OrchestrationComponent.md) - Orchestration layer details
- [CaseFlowEngineComponent.md](./CaseFlowEngineComponent.md) - Case flow engine details
- [SafetyRailsComponent.md](./SafetyRailsComponent.md) - Safety engine details
- [DynamicAgentsComponent.md](./DynamicAgentsComponent.md) - Agent construction details
- [DomainAgentSurfaces.md](./DomainAgentSurfaces.md) - Domain agent and tool details
- [ToolingComponent.md](./ToolingComponent.md) - Tool registry details
- [MemoryLayerComponent.md](./MemoryLayerComponent.md) - Pattern memory details
- [PersistenceComponent.md](./PersistenceComponent.md) - Persistence layer details
- [DomainAgentSurfaces.md](./DomainAgentSurfaces.md) - Details about agent domains and surfaces
- [DomainToolChart.md](./DomainToolChart.md) - Chart of available tools and their domains
- [DynamicAgentsComponent.md](./DynamicAgentsComponent.md) - Details about dynamic agent creation
- [Engine-Rules.md](./Engine-Rules.md) - Rules governing the engine behavior
- [MemoryLayerComponent.md](./MemoryLayerComponent.md) - Details about the memory and pattern storage layer
- [OrchestrationComponent.md](./OrchestrationComponent.md) - Details about orchestration components
- [PersistenceComponent.md](./PersistenceComponent.md) - Details about persistence mechanisms
- [SafetyRailsComponent.md](./SafetyRailsComponent.md) - Details about safety mechanisms
- [Engine-Rules.md](./Engine-Rules.md) - Engine-specific rules and constraints
- [ProjectTerminology.md](./ProjectTerminology.md) - Glossary of terms used in the project

## Overview

SentinelCore is a state-of-the-art multi-agent investigation platform that learns from every case and interaction to speed up future case resolutions. The system learns from the environment and user needs to adapt and improve over time.

## Solution Structure

The solution is organized into a clear layered architecture with strict dependency rules to maintain separation of concerns and prevent architectural drift.

### Projects

The solution consists of the following projects:

| Project | Role | Dependencies |
| --------- | ------ | -------------- |
| `SentinelCore.Contracts` | Shared abstractions, DTOs, settings, events, safety engine — zero project dependencies | Only NuGet packages |
| `SentinelCore.Orchestrations` | Agent construction, orchestration, tools, DI wiring — depends on Contracts only | `SentinelCore.Contracts` |
| `SentinelCore.CaseFlowEngine` | Case lifecycle, persistence, pattern memory — depends on Contracts only | `SentinelCore.Contracts` |
| `SentinelCoreHost` | Host application (console host) | References all three projects |
| `SentinelCore.Tests` | Test project | References all three projects |

### Layer Architecture

The solution follows a strict layered architecture with one-way dependency flow. Dependencies point **upward only** — a lower layer never references a higher layer.

```
┌─────────────────────────────────────────────────────────────┐
│  SentinelCore.Orchestrations / Infrastructure / DI          │  ← wiring layer (knows everything)
│  SentinelCore.CaseFlowEngine / Infrastructure / Persistence  │
├─────────────────────────────────────────────────────────────┤
│  Orchestration                                              │  ← top-level runtime
│  (ISentinelWorkflow implementations: TheCoreOrchestration, │
│   MagneticOrchestration, MagneticCoopOrchestration,         │
│   SingleAgent, GroupConcurrentOrchestration,                │
│   SequentialOrchestration;                                  │
│   selected via IOrchestrationFactory)                        │
├─────────────────────────────────────────────────────────────┤
│  Agents                                                     │  ← agent construction
│  (AgentBuilder, AgentSpecBuilder, Factories, Middleware)    │
├─────────────────────────────────────────────────────────────┤
│  CaseFlow                                                   │  ← case lifecycle
│  (ICaseFlowEngine, CaseFlowEngine)                          │
├─────────────────────────────────────────────────────────────┤
│  Application / Abstractions                                 │  ← orchestration abstractions & tooling
│  (IOrchestrationControl, ToolRegistry)                      │
├─────────────────────────────────────────────────────────────┤
│  Contracts / Abstractions                                   │  ← shared abstractions
│  (ISystemReporter, ICaseRepository, IEvidenceStore,          │
│   IPatternMemoryStore, ISentinelCoreEvents)                 │
├─────────────────────────────────────────────────────────────┤
│  Contracts / SafetyEngine                                   │  ← safety abstractions
│  (ISafetyMiddleware, ISafetyRule, SafetyContext,             │
│   SafetyResult, SafetyVerdict)                              │
├─────────────────────────────────────────────────────────────┤
│  Contracts / CaseFlow                                       │  ← case flow DTOs & interface
│  (Case, CaseStatus, Evidence, Signal, InvestigationPlan,    │
│   InvestigationPlanStep, Resolution, ICaseFlowEngine)       │
├─────────────────────────────────────────────────────────────┤
│  Contracts / Contracts                                      │  ← pure DTOs & settings
│  (SentinelCoreSettings, ModelSettings, OrchestrationType)    │
├─────────────────────────────────────────────────────────────┤
│  Contracts / Events                                         │  ← event types & hub
│  (ISentinelCoreEvents, SentinelCoreEvents, CoreActivityArgs,│
│   MagneticActivityArgs, OrchestrationActivityArgs)          │
├─────────────────────────────────────────────────────────────┤
│  Contracts / DependencyInjection                            │  ← builder interface
│  (ISentinelCoreBuilder)                                     │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules (Immutable)

| Rule | Description |
| ------ | ------------- |
| **Contracts is zero-dependency** | `SentinelCore.Contracts` contains only pure DTOs, settings, abstractions, events, and safety types. It has no project references — only NuGet packages. |
| **Orchestrations depends on Contracts only** | `SentinelCore.Orchestrations` references `SentinelCore.Contracts` for abstractions, events, and settings. It does not reference `SentinelCore.CaseFlowEngine`. |
| **CaseFlowEngine depends on Contracts only** | `SentinelCore.CaseFlowEngine` references `SentinelCore.Contracts` for abstractions and DTOs. It does **not** depend on `SentinelCore.Orchestrations`. |
| **Orchestration abstractions live in Orchestrations** | `ISentinelWorkflow`, `IOrchestrationFactory`, `IOrchestrationControl`, and `IAgentPersona` are defined in `SentinelCore.Orchestrations.Abstractions` — not in Contracts. |
| **Safety abstractions live in Contracts** | `ISafetyMiddleware`, `ISafetyRule`, `SafetyContext`, `SafetyResult`, `SafetyVerdict` are defined in `SentinelCore.Contracts.SafetyEngine`. |
| **CaseFlow depends on Contracts only** | `CaseFlowEngine` implementation depends on `SentinelCore.Contracts.Abstractions` (repositories, safety) and `SentinelCore.Contracts.CaseFlow` (DTOs). It does **not** depend on `Orchestrations`. |
| **Agents depends on Contracts + Orchestrations.Abstractions** | Agent factories use `IAgentBuilder`, `IAgentSpecBuilder`, `AgentSpec`, `AgentRole` (Orchestrations) and `SentinelCoreSettings`, `ModelSettings` (Contracts). Agents never depend on concrete orchestrations. |
| **Orchestration depends on Agents + Contracts** | The top-level runtime layer. May reference agent factories, abstractions, and Contracts types. |
| **Infrastructure/DI depends on all layers** | The wiring layer. Registers everything. May reference all projects. |

### Forbidden Dependency Directions

- Contracts → Orchestrations ❌ (Contracts must be pure)
- Contracts → CaseFlowEngine ❌ (Contracts must be pure)
- CaseFlowEngine → Orchestrations ❌ (CaseFlow must not depend on orchestration)
- Orchestrations → CaseFlowEngine ❌ (Orchestrations must not depend on case flow implementation)

### Namespace Convention

All namespaces use the `SentinelCore.*` root. The namespace **must** match the folder path relative to the project root.

### Key Architectural Patterns

1. **Pattern Memory and Learning**: The system learns from every case and interaction to improve future investigations.
2. **Agent Swarms**: Uses agent swarms to interrogate targets and solve tasks, dividing responsibility between agents to avoid overload.
3. **Layered Architecture**: Strict layering prevents architectural drift and maintains clear separation of concerns.
4. **Dependency Inversion**: Abstractions are defined in contracts, allowing implementations to vary without affecting contracts.
5. **Safety Engine**: Built-in safety mechanisms to prevent harmful or unsafe agent behaviors.

### Key Components

- **Orchestrations**: Responsible for agent construction, orchestration logic, tool management, and dependency injection.
- **CaseFlowEngine**: Manages case lifecycle, persistence, and pattern memory storage.
- **Agents**: Individual AI agents with specific roles and capabilities, built using agent builders and spec builders.
- **Safety Engine**: Provides middleware and rules to ensure agent actions remain safe and within bounds.
- **Events System**: Publishes and subscribes to core activities for monitoring and extensibility.
- **Dependency Injection**: Uses a builder pattern (`ISentinelCoreBuilder`) for configuring and composing the system.

### Projects in Detail

#### SentinelCore.Contracts

- Contains all shared contracts: DTOs, interfaces, settings, events, and safety types.
- Has no dependencies on other solution projects (only NuGet packages).
- Serves as the foundation for all other projects.

#### SentinelCore.Orchestrations

- Contains agent builders, orchestration factories, tool registries, and middleware.
- Depends only on `SentinelCore.Contracts`.
- Defines orchestration abstractions (`ISentinelWorkflow`, `IOrchestrationFactory`, etc.).

#### SentinelCore.CaseFlowEngine

- Implements case lifecycle management, persistence, and pattern memory.
- Depends only on `SentinelCore.Contracts`.
- Provides `ICaseFlowEngine` and `CaseFlowEngine` implementations.

#### SentinelCoreHost

- The console host application that wires up the entire system.
- References all three core projects to compose the final application.

#### SentinelCore.Tests

- Contains unit and integration tests for the solution.
- References all three core projects to test their interactions.

### Documentation Manifest

See [Documentation-Manifest.md](./Documentation-Manifest.md) for a complete list of architectural documents and their purposes.

### Related Documents

- [Pattern Lock](./architecture/pattern-lock.md) - Detailed architectural patterns and rules
- [ComponentList.md](./ComponentList.md) - List of all components and their responsibilities
- [DomainAgentSurfaces.md](./DomainAgentSurfaces.md) - Details about agent domains and surfaces
- [DomainToolChart.md](./DomainToolChart.md) - Chart of available tools and their domains
- [DynamicAgentsComponent.md](./DynamicAgentsComponent.md) - Details about dynamic agent creation
- [Engine-Rules.md](./Engine-Rules.md) - Rules governing the engine behavior
- [MemoryLayerComponent.md](./MemoryLayerComponent.md) - Details about the memory and pattern storage layer
- [OrchestrationComponent.md](./OrchestrationComponent.md) - Details about orchestration components
- [PersistenceComponent.md](./PersistenceComponent.md) - Details about persistence mechanisms
- [SafetyRailsComponent.md](./SafetyRailsComponent.md) - Details about safety mechanisms
- [Engine-Rules.md](./Engine-Rules.md) - Engine-specific rules and constraints
- [ProjectTerminology.md](./ProjectTerminology.md) - Glossary of terms used in the project
