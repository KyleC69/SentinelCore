# SentinelCore Solution Architecture

## Overview

SentinelCore is a multi-agent investigation platform built around MAF workflows, MCP tools, deterministic case-flow services, and a WPF composition root. Pattern-memory storage and matching surfaces exist, but automatic learning and context injection are not complete end-to-end features.

## Solution Structure

The solution is organized into a clear, layered architecture with strict dependency rules to maintain separation of concerns and prevent architectural drift.

### Solution Folders

```text
SentinelCore.slnx
SentinelCore/
├── architecture/                         # Authoritative pattern lock
├── docs/                                 # Documentation and ADRs
└── projects/
    ├── SentinelCore.Contracts/           # Shared abstractions and contracts
    ├── SentinelCore.CaseFlowEngine/      # Case lifecycle and EF Core persistence
    ├── SentinelCore.Orchestrations/      # Agents, workflows, safety, MCP, and DI
    ├── SentinelCore.UI/                  # Active WPF composition root
    ├── SentinelCore.Tests/               # Unit and architecture tests
    └── SentinelCoreService/              # Windows service scaffold
SentinelCoreSecurity/                     # Message-provenance security module; sibling of SentinelCore/, solution member
```

### Projects and Their Responsibilities

| Project | Role | Project dependencies |
| --------- | ------ | -------------- |
| `SentinelCore.Contracts` | Shared contracts, settings, events, MCP definitions, and abstractions | None |
| `SentinelCore.CaseFlowEngine` | Optional EF Core-backed case lifecycle and persistence | `SentinelCore.Contracts` |
| `SentinelCore.Orchestrations` | Agent factory, workflows, executors, safety rules, MCP, RAG, and DI registration | `SentinelCore.Contracts`, `SentinelCore.CaseFlowEngine`, `SentinelCoreSecurity` |
| `SentinelCore.Security` (project `SentinelCoreSecurity`) | Message provenance: HMAC-SHA256 signing, identity registry, fail-closed validation, quarantine | `SentinelCore.Contracts` |
| `SentinelCore.UI` | Active WPF host and composition root | All library projects (`SentinelCore.Contracts`, `SentinelCore.CaseFlowEngine`, `SentinelCore.Orchestrations`, `SentinelCoreSecurity`) |
| `SentinelCore.Tests` | Unit and architecture validation | Solution libraries |
| `SentinelCoreService` | Windows service host scaffold; excluded from normal solution build | Solution libraries |

### Dependency Graph

```text
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

```text
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
│  (SentinelAgentFactory, AgentPresetDefinition,              │
│   ModelResolver, AgentToolSelection, SafetyRuleEngine)       │
├─────────────────────────────────────────────────────────────┤
│  Executors                                                   │
│  (SafetyExecutor, SafetyReviewExecutor, PatternCheck,         │
│   WhiteList, TheCoreSwitch, SignalClassifier, NewCase,        │
│   DirectAnswer, TerminateWorkflow, SubWorkflow)               │
├─────────────────────────────────────────────────────────────┤
│  CaseFlow                                                    │
│  (ICaseFlowEngine, CaseFlowEngine)                           │
├─────────────────────────────────────────────────────────────┤
│  Contracts / Abstractions                                    │
│  (ISystemReporter, ICaseFlowEngine, IEvidenceStore,          │
│   ISignalRepository, IPatternMemoryStore, IMcpServerRegistry, │
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
| **Orchestration abstractions live in Orchestrations** | Current orchestration, agent-factory, and tool-selection interfaces are defined in `SentinelCore.Orchestrations.Abstractions`. |
| **Safety types live in Orchestrations** | `ISafetyRule`, `SafetyEngineAgent`, `SafetyEvaluationContext`, `SafetyRuleResult`, `SafetyAction`, `SafetySeverity` are in `SentinelCore.Orchestrations.SafetyEngine`. |
| **Security depends on Contracts only** | `SentinelCoreSecurity` references `SentinelCore.Contracts` (plus AI/DI abstraction packages). Orchestrations, UI, and Tests may reference it; it must never reference a higher layer. |

### Forbidden Dependency Directions

- Contracts → Orchestrations ❌ (Contracts must be pure)
- Contracts → CaseFlowEngine ❌ (Contracts must be pure)
- CaseFlowEngine → Orchestrations ❌ (CaseFlow must not depend on orchestration)
- SentinelCoreSecurity → Orchestrations ❌ (the security module stays at the contract boundary)
- SentinelCoreSecurity → CaseFlowEngine ❌
- SentinelCoreSecurity → UI/Hosts ❌

### Namespace Convention

All namespaces use the `SentinelCore.*` root. Namespaces **must** match the folder path relative to the project root.

Key namespace mappings:

| Project | Namespaces |
| --------- | ----------- |
| Contracts | `SentinelCore.Abstractions`, `SentinelCore.CaseFlow`, `SentinelCore.CaseEngine`, `SentinelCore.Contracts`, `SentinelCore.Events`, `SentinelCore.DependencyInjection` |
| CaseFlowEngine | `SentinelCore.CaseFlowEngine.CaseFlow`, `SentinelCore.CaseFlowEngine.Infrastructure.Persistence`, `SentinelCore.CaseFlowEngine.Infrastructure.DependencyInjection` |
| Orchestrations | `SentinelCore.Agents`, `SentinelCore.Application`, `SentinelCore.Orchestrations`, `SentinelCore.SafetyEngine`, `SentinelCore.Tools`, `SentinelCore.Workflows`, `SentinelCore.Workflows.Executors`, `SentinelCore.Personas`, `SentinelCore.Infrastructure.DependencyInjection` |
| SentinelCoreSecurity | `SentinelCore.Security`, `SentinelCore.Security.Cryptography`, `SentinelCore.Security.Identity`, `SentinelCore.Security.Quarantine`, `SentinelCore.Security.Infrastructure.DependencyInjection` |
| Host (UI) | `SentinelCore.UI`, `SentinelCore.UI.ViewModels`, `SentinelCore.UI.Views`, `SentinelCore.UI.Services`, `SentinelCore.UI.Converters`, `SentinelCore.UI.Models` |

### Key Architectural Patterns

1. **Workflow-based orchestration** - `TheCoreWorkflow` composes a safety gate, pattern check, whitelist gate, routing switch, and a Magentic investigation sub-workflow.
2. **Preset-based agent construction** - `SentinelAgentFactory` resolves an `AgentPreset`, model tier, instructions, and explicit tool selection before constructing the MAF agent.
3. **Optional persistence** - library registration installs null-object case-flow services; a host opts into EF Core with `AddCaseFlowEngine` and registers `DbContext` factories itself.
4. **Factory-managed persistence lifetimes** - persistence consumers use `IDbContextFactory<TContext>` and create one short-lived context per operation.
5. **Rule-based safety executor** - the active workflow evaluates input through `SafetyExecutor` and `SafetyRuleEngine`. Middleware-based safety agent types remain separate surfaces and are not the active workflow gate.
6. **Live MCP tool selection** - MCP and local tools are merged at agent creation and filtered through `AgentToolSelection` domain or name rules.
7. **Host-owned logging and telemetry** - library services publish through `ISystemReporter` and the host owns logging and OpenTelemetry configuration.
8. **Fail-closed message provenance** - every message entering The Core workflow is HMAC-signed at the UI entry point, validated at the workflow entry gate (nonce-consuming), and re-checked at the model boundary; rejected messages are quarantined unchanged and never reach a model. See [SecurityComponent.md](./SecurityComponent.md).

### Key Components

- **Contracts** - Shared interfaces, models, configuration, events, and MCP contracts; no solution project dependencies.
- **CaseFlowEngine** - Optional case lifecycle and EF Core persistence, registered by a composition root when required.
- **Orchestrations** - Agent construction, model resolution, workflow and executor composition, safety rules, MCP integration, RAG services, and DI extensions.
- **Agents** - Core, manager, classifier, safety, worker, utility, and aggregator presets created through `ISentinelAgentFactory`.
- **Safety** - Thirteen default `ISafetyRule` implementations in the workflow safety executor, plus the separate safety-agent rule engine surface.
- **Workflows** - `TheCoreWorkflow` is the configured production orchestration; `CustomGroupWorkflow` remains an additional group-workflow implementation.
- **Events** - `ISystemReporter` distributes diagnostics to available reporters and `ISentinelCoreEvents` carries application-facing activity.
- **Host** - `SentinelCore.UI` owns host startup, model configuration, EF registration, MCP initialization, navigation, and shutdown.

### Projects in Detail

#### SentinelCore.Contracts

- Defines the shared configuration, event, MCP, persistence, reporting, and case abstractions consumed by the other projects.
- Has no project references. Package dependencies do not change its architectural role as the foundation layer.

#### SentinelCore.Orchestrations

- Contains `SentinelAgentFactory`, `SentinelChatClientFactory`, model-tier resolution, `TheCoreWorkflow`, workflow executors, MCP lifecycle management, RAG services, safety rules, and service registration.
- References both `SentinelCore.Contracts` and `SentinelCore.CaseFlowEngine`.

#### SentinelCore.CaseFlowEngine

- Contains the EF Core implementation of `ICaseFlowEngine` and the case-related persistence stores.
- References only `SentinelCore.Contracts`.
- The module is optional at library level and is enabled explicitly by the composition root.

#### SentinelCore.Security (SentinelCoreSecurity)

- Message-provenance security module: HMAC-SHA256 stamping of `ChatMessage` via `AdditionalProperties`, fail-closed validation, an in-memory identity registry (stub), and a bounded in-memory quarantine store. See [SecurityComponent.md](./SecurityComponent.md) and [ADR-008](./ADR/008-Message-Provenance-Security-Module.md).
- References only `SentinelCore.Contracts`. Consumed by `SentinelCore.Orchestrations` (workflow entry gate and model-boundary re-check), `SentinelCore.UI` (entry-point stamping), and `SentinelCore.Tests`.
- Source lives in the sibling solution folder `SentinelCoreSecurity/`, outside the repository root.

#### SentinelCore.UI

- Active WPF host and composition root.
- References all three library projects and owns host configuration, model persistence, MCP startup, UI navigation, and shutdown.

#### SentinelCore.Tests

- Unit, component, and architecture tests for the current solution. It is excluded from the normal solution build in the solution file.

#### SentinelCoreService

- A .NET Framework 4.8 Windows service scaffold with build dependencies on the library projects. It is excluded from the normal Debug solution build.

### Related Documents

- [ContractsComponent.md](./ContractsComponent.md) - Contracts layer details
- [OrchestrationComponent.md](./OrchestrationComponent.md) - Orchestration layer details
- [CaseFlowEngineComponent.md](./CaseFlowEngineComponent.md) - Case flow engine details
- [SafetyRailsComponent.md](./SafetyRailsComponent.md) - Safety engine details
- [DynamicAgentsComponent.md](./DynamicAgentsComponent.md) - Agent construction details
- [ToolingComponent.md](./ToolingComponent.md) - Current tool-group and selection design
- [MemoryLayerComponent.md](./MemoryLayerComponent.md) - Pattern-memory implementation status
- [PersistenceComponent.md](./PersistenceComponent.md) - Persistence and DbContext factory rules
- [ProjectTerminology.md](./ProjectTerminology.md) - Canonical terminology

## Current Host and Registration Flow

The WPF host is the active composition root. It calls `AddSentinelCore`, opts into persistence with `AddCaseFlowEngine`, registers `AddDbContextFactory<SentinelCoreDBContext>`, and registers its UI services through `AddSentinelCoreUi`. The library registration alone uses null-object implementations for optional case-flow persistence; hosts decide whether the EF Core-backed module is enabled.

```text
SentinelCore.UI
    |
    +-- AddSentinelCore
    |       +-- orchestration and agent factory
    |       +-- workflow and executor registrations
    |       +-- MCP registry and in-process tools
    |       +-- null case-flow defaults
    |
    +-- AddCaseFlowEngine
    |       +-- EF Core-backed case lifecycle services
    |
    +-- AddDbContextFactory<SentinelCoreDBContext>
            +-- per-operation context creation
```

## Authoritative References

- [Pattern Lock](../architecture/pattern-lock.md) - Authoritative architectural constraints
- [Documentation Manifest](./Documentation-Manifest.md) - Document inventory, status, and classification
- [Project Terminology](./ProjectTerminology.md) - Canonical domain and system terms
- [Orchestration Component](./OrchestrationComponent.md) - Current workflow and execution surfaces
- [Tooling Component](./ToolingComponent.md) - Tool domains and agent tool selection
- [Persistence Component](./PersistenceComponent.md) - EF Core storage and lifetime rules
- [Safety Engine Component](./SafetyEngineComponent.md) - Current safety rules and execution status

Use this document when evaluating the current solution boundaries, selecting an integration point, or reviewing a proposed architectural change.

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 2.0.0 | unknown | unknown | Removed duplicated and contradictory architecture descriptions; aligned project membership, dependencies, host registration, and authoritative links with the current codebase. |
| 2.1.0 | 2026-11-20 | Copilot | Added the SentinelCoreSecurity message-provenance module: folder tree, project table, dependency rules, forbidden directions, namespaces, project details, and the fail-closed provenance pattern. |
