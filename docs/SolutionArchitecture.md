# SentinelCore Solution Architecture

## Overview

SentinelCore is a state-of-the-art multi-agent investigation platform designed to learn from every case and interaction to accelerate future case resolutions. The system adapts to the user's environment and needs, utilizing local or premium cloud models without requiring special configuration or adapters.

The platform distinguishes itself through its pattern memory and the use of agent swarms to interrogate targets and solve tasks. By dividing responsibilities among agents, the system avoids overloading individual agents with too many constraints or tasks.

## Solution Structure

The solution is organized into a clear, layered architecture with strict dependency rules to maintain separation of concerns and prevent architectural drift.

### Solution Folders

```
Solution-Root/
├── architecture/          # Architectural decisions and pattern locks
├── assets/                # Static assets (images, etc.)
├── docs/                  # Documentation (this folder)
├── projects/              # Source code projects
│   ├── Console/                   # External terminal for logging output
│   ├── SentinelCore.CaseFlowEngine/ # Case lifecycle, persistence, pattern memory
│   ├── SentinelCore.Contracts/    # Shared abstractions, DTOs, settings, events, safety
│   ├── SentinelCore.Orchestrations/ # Agent construction, orchestration, tools, DI
│   ├── SentinelCore.Tests/        # Unit and integration tests
│   └── SentinelCoreHost/          # Host application (WPF host)
└── Output/                # Build output (bin, obj folders for each project)
```

### Projects and Their Responsibilities

| Project | Role | Dependencies |
|---------|------|--------------|
| `SentinelCore.Contracts` | Shared abstractions, DTOs, settings, events, safety engine — zero project dependencies | None (only NuGet packages) |
| `SentinelCore.Orchestrations` | Agent construction, orchestration, tools, DI wiring — depends on Contracts only | `SentinelCore.Contracts` |
| `SentinelCore.CaseFlowEngine` | Case lifecycle, persistence, pattern memory — depends on Contracts only | `SentinelCore.Contracts` |
| `SentinelCoreHost` | Host application (WPF) that wires up the entire system | All three core projects |
| `Console` | Agent Communication logging | All three core projects |
| `SentinelCore.Tests` | Unit and integration tests | All three core projects |

### Layer Architecture

The solution follows a strict layered architecture with **one-way dependency flow**. Dependencies point **upward only** — a lower layer never references a higher layer.

```
┌─────────────────────────────────────────────────────────────┐
│  SentinelCore.Orchestrations / Infrastructure / DI          │  ← wiring layer (knows everything)
│  SentinelCore.CaseFlowEngine / Infrastructure / Persistence  │
├─────────────────────────────────────────────────────────────┤
│  Orchestration                                              │  ← top-level runtime
│  (ISentinelWorkflow implementations: TheCoreOrchestration, │
│   MagneticOrchestration, MagneticCoopOrchestration,         │
│   SingleAgent, GroupConcurrentOrchestration,                │
│   GroupTurnBasedOrchestration, SequentialOrchestration;      │
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
│   IPatternStore, ISafetyMiddleware, ISafetyRule,            │
│   SafetyContext, SafetyResult, SafetyVerdict,               │
│   SentinelCoreSettings, ModelSettings,                      │
│   CaseStarted, CaseUpdated, CaseClosed, EvidenceAdded,      │
│   PatternLearned, AgentThought, AgentAction, ToolUsed)      │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules (Immutable)

| Rule | Description |
|------|-------------|
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

- The WPF host application that wires up the entire system.
- References all three core projects to compose the final application.

#### Console

- The console host application.
- References all three core projects to compose the final application.

#### SentinelCore.Tests

- Contains unit and integration tests for the solution.
- References all three core projects to test their interactions.

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

## Overview

SentinelCore is a state-of-the-art multi-agent investigation platform that learns from every case and interaction to speed up future case resolutions. The system learns from the environment and user needs to adapt and improve over time.

## Solution Structure

The solution is organized into a clear layered architecture with strict dependency rules to maintain separation of concerns and prevent architectural drift.

### Projects

The solution consists of the following projects:

| Project | Role | Dependencies |
|---------|------|--------------|
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
|------|-------------|
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
