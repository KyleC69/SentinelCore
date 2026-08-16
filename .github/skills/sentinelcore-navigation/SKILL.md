---
name: sentinelcore-navigation
description: "Search and navigate the SentinelCore repository effectively. Use when agents need to find code, understand project structure, locate agents/tools/workflows/middleware/DI/safety-rules, trace execution paths, or understand how the SentinelCore codebase is organized. Covers project layout, layer dependencies, naming conventions, key file locations, agent construction pipeline, tool registry, workflow executors, safety engine, and search strategies for this specific repo."
context: fork
---

# SentinelCore Repository Navigation

SentinelCore is a Windows-based AI security investigation platform built on Microsoft Agent Framework (MAF) and OllamaSharp. This skill teaches agents how to efficiently search and navigate this specific repository.

## Repository at a Glance

| Fact | Value |
|------|-------|
| **Root** | `f:\Solutions\SentinelCore\SentinelCore\` |
| **Language** | C# (.NET 10) |
| **Framework** | Microsoft Agent Framework 1.17.0, OllamaSharp 5.4.30 |
| **App type** | WPF (SentinelCoreAdmin is the active host; SentinelCoreHost is being phased out) |
| **Package management** | Central Package Management (`Directory.Packages.props`) |
| **Build artifacts** | `F:\Artifacts\$(ProjectName)` (via `Directory.Build.props`) |
| **Test framework** | MSTest 4.2.3 + Moq 4.20.72 |
| **No .sln file** | Solution managed via `.code-workspace` file |

## Project Layout

All projects live under `projects/`. Dependencies point **upward only** (lower layers never reference higher layers).

```
projects/
├── SentinelCore.Contracts/          # Layer 0: Pure DTOs, interfaces, domain models (zero deps)
├── SentinelCore.CaseFlowEngine/     # Layer 1: Case lifecycle, EF Core persistence (→ Contracts)
├── SentinelCore.Orchestrations/     # Layer 2: Agents, tools, workflows, safety, DI (→ CaseFlow + Contracts)
├── SentinelCoreHost/                # Host (phasing out) — WPF app (→ Orchestrations + Contracts + CaseFlow)
├── SentinelCoreAdmin/               # Host (active) — WPF admin UI (→ Admin.Core + Contracts + Orchestrations + CaseFlow)
├── SentinelCoreAdmin.Core/          # Admin support — Identity, Graph services (netstandard2.0)
└── SentinelCore.Tests/              # Tests — MSTest + Moq (→ Contracts + CaseFlow + Orchestrations)
```

### Layer Dependency Rules (Critical)

| Layer | May depend on |
|-------|--------------|
| `Contracts/` | Nothing (pure DTOs) |
| `CaseFlowEngine/` | `Contracts` only |
| `Orchestrations/` | `CaseFlowEngine` + `Contracts` |
| `Host/Admin` | `Orchestrations` + `Contracts` + `CaseFlowEngine` |

**Forbidden**: Contracts→Application, Domain→Agents, CaseFlow→Agents, Agents→Orchestration, any lower→higher.

## Where to Find Things — Quick Lookup

| Looking for... | Go to... |
|----------------|----------|
| Agent construction pipeline | `Orchestrations\Agents\SentinelAgentFactory.cs` |
| Agent profiles/roles | `Orchestrations\Agents\AgentProfile.cs`, `AgentRole.cs`, `AgentProfileBuilder.cs` |
| Chat client creation per provider | `Orchestrations\Agents\SentinelChatClientFactory.cs` |
| The Core agent workflow | `Orchestrations\Workflows\TheCoreWorkflow.cs` |
| The Core executor (persistent session) | `Orchestrations\Workflows\Executors\TheCoreExec.cs` |
| Manager orchestration | `Orchestrations\Orchestrations\MagneticOrchestration.cs` |
| All Windows diagnostic tools | `Orchestrations\Tools\*.cs` (40+ files) |
| Tool registry (domain→tool mapping) | `Orchestrations\Application\ToolRegistry.cs` |
| MCP tools (Core agent only) | `Orchestrations\Agents\Core\Tools\` |
| Safety engine | `Orchestrations\SafetyEngine\` |
| Safety rules (15 implementations) | `Orchestrations\SafetyEngine\Rules\*.cs` |
| Client middleware | `Orchestrations\Agents\Middleware\` |
| DI registration entry point | `Orchestrations\Infrastructure\DependencyInjection\SentinelCoreServiceExtensions.cs` |
| Case lifecycle engine | `CaseFlowEngine\Cfe\CaseFlowEngine.cs` |
| EF Core entities & mappings | `CaseFlowEngine\Persistence\*.cs` |
| Settings/config models | `Contracts\Contracts\SentinelCoreSettings.cs`, `ModelProfile.cs` |
| Event hub | `Contracts\Events\ISentinelCoreEvents.cs`, `SentinelCoreEvents.cs` |
| Personas (33 predefined) | `Orchestrations\Personas\PersonaRegistry.cs` |
| Workflow executors (20+) | `Orchestrations\Workflows\Executors\*.cs` |
| Host startup (active) | `SentinelCoreAdmin\App.xaml.cs` |
| Host startup (legacy) | `SentinelCoreHost\App.xaml.cs` |
| Tests | `SentinelCore.Tests\*Tests.cs` |
| Test infrastructure (fakes) | `SentinelCore.Tests\TestInfrastructure\` |
| Architecture rules | `.github\instructions\drift-prevention.instructions.md` |
| Terminology | `.github\instructions\ProjectTerminology.instructions.md` |
| Architecture docs | `docs\SolutionArchitecture.md` |

> All paths above are relative to `projects/` unless they start with `.github\` or `docs\`.

## Naming Conventions

| Pattern | Example | What it is |
|---------|---------|------------|
| `*ReadTool.cs` | `RegistryReadTool.cs`, `FirewallReadTool.cs` | Read-only diagnostic AITool (~35 files) |
| `*Tool.cs` | `AuditingTool.cs`, `CreateCaseTool.cs`, `WmiQueryTool.cs` | Non-read AITool (~5 files) |
| `*Executor.cs` | `InvestigationExecutor.cs`, `SafetyExecutor.cs` | Workflow executor (~20 files) |
| `*Extensions.cs` | `SentinelCoreServiceExtensions.cs` | DI/builder extension methods |
| `*Builder.cs` | `AgentProfileBuilder.cs`, `SentinelCoreBuilder.cs` | Builder pattern class |
| `*Factory.cs` | `SentinelAgentFactory.cs`, `SentinelChatClientFactory.cs` | Factory class |
| `*Entity.cs` | `CaseEntity.cs`, `EvidenceEntity.cs` | EF Core entity |
| `*MappingExtensions.cs` | `CaseMappingExtensions.cs` | EF Core entity→domain mapping |
| `*EventArgs.cs` | `OrchestrationOutputEventArgs.cs` | Event args class |
| `*Tests.cs` | `AgentFactoryTests.cs` | MSTest test class |
| `*Exception.cs` | `SentinelOrchestrationException.cs` | Custom exception |

### Namespace Convention

- Root namespace: `SentinelCore` (shared by Contracts, CaseFlowEngine, Orchestrations)
- Namespace **must match** the folder path relative to the project root
- Exception: `SentinelCoreHost`, `SentinelCoreAdmin`, `SentinelCoreAdmin.Core` use their own root namespaces

## Key Architectural Patterns

### Agent Construction Pipeline

```
AgentRole (enum: Core, Manager, Utility)
    ↓
AgentProfileBuilder.Build(role) → AgentProfile (sealed record)
    ↓
SentinelAgentFactory.Create(profile) → AIAgent
    ↓
SentinelChatClientFactory.CreateChatClient(model) → IChatClient
    ↓ (wrapped with middleware per AgentMiddlewarePipeline preset)
ChatClientAgent (constructed by factory, never directly)
```

**Never construct `ChatClientAgent` directly.** Always go through `ISentinelAgentFactory`.

### Middleware Pipeline Presets

| Preset | Logging | Events | Safety | PatternMemory | NullSafety | Tools |
|--------|---------|--------|--------|---------------|------------|-------|
| `Core` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Default` | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| `Domain` | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| `Manager` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `Minimal` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

### Tool Pattern

- All tools extend `AITool` directly (e.g., `public sealed class RegistryReadTool : AITool`)
- Every tool **must** return `ToolResult` (`ToolResult.Ok()` / `ToolResult.Fail()`)
- Tools are **NOT** registered in DI — instantiated by domain agents as needed
- `ToolRegistry` is a static mapping of domain names → tool class names (43 domains)
- Tool parameters use `[Description]` attributes for AI consumption

### Event System

All agent output flows through `ISentinelCoreEvents` — **never** use `Console.WriteLine` in the library.

### Session Model

| Agent type | Session |
|------------|---------|
| Core Agent | Isolated persistent session (RAG, middleware, pattern memory) |
| Manager | Workflow session shared with subagents (no RAG, no enrichment) |
| Domain/Composite | Stateless — receives only skill, task, and toolbelt per invocation |

## Search Strategies

### Finding an agent's tools
1. Check `AgentRole` → look up `AgentMiddlewarePipeline` preset
2. Check `ToolRegistry.cs` for domain→tool mapping
3. For Core agent MCP tools: `Orchestrations\Agents\Core\Tools\`
4. For all Windows diagnostic tools: `Orchestrations\Tools\*.cs`

### Tracing an execution path
1. Start at `OrchestrationControl.cs` (entry point)
2. → `TheCoreWorkflow.cs` (signal classification & routing)
3. → `Executors\*.cs` (individual workflow steps)
4. → `MagneticOrchestration.cs` (Manager's investigation sub-workflow)
5. → `SentinelAgentFactory.cs` (how agents are built for each step)

### Finding DI registrations
1. `SentinelCoreServiceExtensions.cs` — `AddSentinelCore()` is THE entry point
2. `ExecutorRegistration.cs` — registers all executors
3. `CaseFlowEngineBuilderExtensions.cs` — CFE registrations
4. Host: `ServiceCollectionRegistrationExtensions.cs` in each host project

### Finding safety rules
1. `SafetyEngine\ISafetyRule.cs` — interface
2. `SafetyEngine\Rules\*.cs` — 15 rule implementations
3. `SafetyEngineAgent.cs` — middleware agent that evaluates rules
4. `SafetyEngineAgentBuilderExtensions.cs` — `UseSafetyEngine()` extension

### Finding documentation
1. `docs\SolutionArchitecture.md` — full architecture overview
2. `docs\*Component.md` — per-component design docs
3. `docs\DomainToolChart.md` — domain→tool mapping chart
4. `.github\instructions\drift-prevention.instructions.md` — architectural rules
5. `.github\instructions\ProjectTerminology.instructions.md` — canonical terms

For the full detailed repo map with every file, see [references/repo-map.md](references/repo-map.md).
For detailed search strategies and navigation workflows, see [references/search-strategies.md](references/search-strategies.md).

## Key Files to Read First

When onboarding to this repo for the first time, read in this order:

1. `.github\instructions\ProjectTerminology.instructions.md` — canonical terms
2. `.github\instructions\drift-prevention.instructions.md` — architectural rules
3. `docs\SolutionArchitecture.md` — full architecture overview
4. `Orchestrations\Infrastructure\DependencyInjection\SentinelCoreServiceExtensions.cs` — DI entry point
5. `Orchestrations\Agents\SentinelAgentFactory.cs` — agent construction pipeline
6. `Orchestrations\Workflows\TheCoreWorkflow.cs` — main workflow
7. `Contracts\Contracts\SentinelCoreSettings.cs` — settings model

## Global Usings

**Orchestrations** (`GlobalUsings.cs`):
```csharp
global using Microsoft.Extensions.AI;
global using Microsoft.Agents.AI;
global using Microsoft.Agents.AI.Workflows;
global using SentinelCore.Contracts;
global using SentinelCore.Orchestrations;
```

**Tests** (`GlobalUsings.cs`):
```csharp
global using Microsoft.Agents.AI;
global using Microsoft.Extensions.AI;
global using SentinelCore.Agents;
global using SentinelCore.Cfe;
global using SentinelCore.Contracts;
global using SentinelCore.Events;
global using SentinelCore.Personas;
global using SentinelCore.Tools;
```

## Build Commands

| Task | Command |
|------|---------|
| Build (legacy host) | `dotnet build projects/SentinelCoreHost/SentinelCoreHost.csproj` |
| Build (admin) | `dotnet build projects/SentinelCoreAdmin/SentinelCoreAdmin.csproj` |
| Run tests | `dotnet test projects/SentinelCore.Tests/SentinelCore.Tests.csproj` |
| Watch (legacy host) | `dotnet watch run --project projects/SentinelCoreHost/SentinelCoreHost.csproj` |

> **Note**: Capture large command output to a temp file first (e.g., `dotnet build --tl:off 2>&1 | Out-File $env:TEMP\build.log`), then analyze the file.
