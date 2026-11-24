

Completed: *Compile findings report* (7/7)

# Architectural Review — SentinelCore Solution

## 1. Decision Summary

The solution has a **credible layered intent** (Contracts → CaseFlowEngine → Orchestrations → UI) undermined by pervasive **contract leakage, service-locator patterns, duplicate types, and unfinished subsystems wired into the production graph**. The single biggest structural defect: **the case-lifecycle contract (`ICaseFlowEngine`) and its entity types live in the implementation project**, so every consumer (Orchestrations, UI) depends on the persistence internals rather than an abstraction. The second: **`IServiceProvider` is threaded through workflow objects**, making the executor graph a hidden service-locator composition rather than an explicit one. The codebase also carries **three near-identical `ToolResult` types, two `InvestigationObjective` types, and drifted null-objects** — evidence that "one canonical location per type" is aspirational, not real.

Overall grade: **C+ architecture with B-tier intent** — the pattern documentation is ahead of the code.

---

## 2. Observed Principle Violations (evidence-based)

### Critical

| # | Finding | Evidence |
| --- | --------- | ---------- |
| C1 | **Contract lives in the implementation project.** `ICaseFlowEngine` is declared inside `CaseFlowEngine.cs` (implementation file) in `SentinelCore.CaseFlowEngine`, alongside the concrete `CaseFlowEngine`. The seam abstraction the docs promise (`SentinelCore.Contracts.CaseFlow.ICaseFlowEngine`, per `docs/CaseFlowEngineComponent.md`) does not exist in code. Downstream projects consume persistence internals directly. | `projects/SentinelCore.CaseFlowEngine/CaseFlow/CaseFlowEngine.cs:26` |
| C2 | **Entity types cross every boundary.** `SignalEntity`, `CaseEntity` (EF-persistence types with `[Table]` attributes) are used by `NewCaseExecutor` (`Orchestrations/Workflows/Executors/NewCaseExecutor.cs` — `using SentinelCore.CaseFlowEngine.DBEntities`) and by `CoreChatViewModel` in the UI. The mapping helpers (`Signal.ToEntity()` in `CaseEntityExtensions.cs`) live in the **persistence project's** `Persistence/` folder, forcing the business layer to reference persistence just to convert its own DTOs. | `NewCaseExecutor.cs:8–16`, `CoreChatViewModel.cs:18` |
| C3 | **Null-object violates its own contract.** `NullCaseFlowEngine` — documented "Never throws — every member is a safe no-op" — throws `NotImplementedException()` from `CreateCase(SignalEntity)` and `GetAllowedTransitions(int)`. The class also carries **extra public members not on the interface** (`CreateCase(Signal)`, `CreateCaseAsync(Signal)`, `GetAllowedTransitions(CaseStatus)`, `GetCasesByStatusAsync`) — the interface and null-object have drifted apart, indicating two generations of API coexist. | `Persistence/NullPersistenceServices.cs:44,74` vs interface in `CaseFlowEngine.cs:26–148` |
| C4 | **Service locator inside the workflow engine.** `TheCoreWorkflow` accepts `IServiceProvider` solely to construct `ExecutorFactory`, which builds all 14 executors via `ActivatorUtilities.CreateInstance<T>` (`ExecutorFactory.cs:43`). Executors are *not* DI-registered (only partially via `RegisterExecutors`), so their lifetimes, dependencies, and construction failures are opaque. Consequence: `SafetyExecutor`'s `IEnumerable<ISafetyRule>? rules` parameter is **dead** — DI can never supply it through `ActivatorUtilities`, so the 13 hardcoded static default rules are effectively immovable. | `TheCoreWorkflow.cs:99–107`, `SentinelCoreServiceExtensions.cs:152` |

### High

| # | Finding | Evidence |
| --- | --------- | ---------- |
| H1 | **Mutable shared preset mutation.** `AgentPresetDefinition` is a `record` with **settable** `AgentId`/`AgentName`/`Description`, and `SentinelAgentFactory.CreateAgentAsync(presetName, agentName, agentDescription, …)` **mutates the resolved definition** (`preset.AgentName = agentName;`) before use. If `FromName` returns any shared/cached instance, this is a cross-call state-corruption bug. Records must be immutable; overrides should be `with`-copies. | `SentinelAgentFactory.cs:206–208`, `AgentPresetDefinition.cs:44–53` |
| H2 | **"Zero-dependency" Contracts is polluted.** Contracts references `Microsoft.Agents.AI.Abstractions`, `Microsoft.Data.SqlClient`, `Microsoft.Extensions.Logging`; `IMcpServerRegistry` exposes MAF `AITool`; `PatternMemory` uses `Microsoft.Data.SqlTypes` (SQL Server vector types); and `SystemReporter` — a concrete implementation — ships inside the contracts assembly. The contract layer is coupled to (a) the AI framework, (b) a specific database vendor, (c) runtime implementations. | `Contracts.csproj:12–18`, `IMcpServerRegistry.cs:9,26`, `PatternMemory.cs:9`, `Abstractions/SystemReporter.cs` |
| H3 | **Type duplication.** `InvestigationObjective` exists twice (Contracts.CaseFlow and Orchestrations.Infrastructure, near-identical, both carrying persistence attribute `[DatabaseGenerated]`); `ToolResult` exists three times with different shapes (`SentinelCoreMCP.Tools.ToolResult` w/ `ErrorCategory`, `AgentAILib.ToolFunctions.Utils.ToolResult<T>`, `SentinelCore.Orchestrations...ToolResult`). Also `Signal` (Contracts) vs `SignalEntity` (DB) plus **both overloads on the engine interface** — the domain/persistence boundary is half-done. | `Contracts/CaseFlow/InvestigationObjective.cs` vs `Orchestrations/Infrastructure/InvestigationObjective.cs`; `Tools/ToolResult.cs`, `Utils/ToolResult.cs`, `Orchestrations/Agents/Core/Tools/ToolResult.cs` |
| H4 | **Namespace drift.** Three files in MCPTools use a foreign root namespace `AgentAILib.ToolFunctions.Utils`; `CaseStatus` lives in `SentinelCore.Contracts.Cfe` while every sibling is `SentinelCore.Contracts.CaseFlow`; the MCP project has **three identities** (folder `SentinelCore.MCPTools`, csproj `SentinelCore.MCP.csproj`, root namespace/package `SentinelCoreMCP`). | `grep namespace (?!SentinelCore)` → 3 hits; `CaseStatus.cs:13` |
| H5 | **Unfinished subsystems wired into the production graph.** `PersistEvidence` executor is a documented stub that passes messages through (`// TODO: Implement evidence persistence logic`) yet sits in the compiled graph (`ExecutorCollection`); `SemanticPatternMatcher.SearchAsync` logs a TODO and **returns empty always**; `RagSearchService` is an in-memory keyword toy; `CaseUpdateExecutor` is a TODO; persistence for `InsertReport`/`InsertPatternSummaries` tools is a no-op that reports success to the model. Agents believe operations succeed when nothing happened — a correctness hazard in an investigation platform. | `PersistEvidence.cs:20,76`, `SemanticPatternMatcher.cs:83–101`, `CaseEngineTools.cs:75–95` |
| H6 | **`WorkflowBase` prints to console** — 50+ `Console.WriteLine` calls for agent transcripts, traces, and Magentic ledgers in a WPF-hosted library, bypassing the reporting abstraction. Also: `protected IWorkflowEventProcessor _eventProcessor` field is never assigned in the base constructor — NRE by design until derived classes remember. | `WorkflowBase.cs:131–710` (56 hits), field at `WorkflowBase.cs:29` |
| H7 | **Dual eager/lazy initialization with two synchronization mechanisms.** `TheCoreWorkflow` holds both `object _initLock` + `volatile bool _isInitialized` **and** `SemaphoreSlim _initializationGate`; non-nullable agent fields (`_adversarialAnalystAgent`, `_managerAgent`…) are actually null until `InitializeAsync` runs — nullable annotations contradicted by real lifecycle. Same pattern duplicated in `CustomGroupWorkflow`. | `TheCoreWorkflow.cs:33–58`, `CustomGroupWorkflow.cs:59–75` |

### Medium

| # | Finding | Evidence |
| --- | --------- | --------- |
| M1 | **Machine-specific paths in version control.** Post-build/publish targets copy to `F:\Builds\$(ProjectName)` and `Released`. Breaks every other machine's build. | `Orchestrations.csproj` (`CopyBuildOutput` target), `SentinelCore.MCP.csproj` |
| M2 | **Out-of-tree project references.** Orchestrations and Tests reference SentinelCoreSecurity via `..\..\..\..\SentinelCoreSecurity\...` — escaping the repo root; the Security module carries its **own `Directory.Build.props`/`Directory.Packages.props`** (version duplication ⇒ drift risk). | `Orchestrations.csproj:22`, `SentinelCore.Tests.csproj:41`, `SentinelCoreSecurity/Directory.Packages.props` |
| M3 | **Version pinning duplicated.** MCP csproj hardcodes all four version properties (1.0.12) that `Directory.Build.props` already centralizes; bumping the version requires two edits in different trees. | `SentinelCore.MCP.csproj:31–34` |
| M4 | **`SentinelCoreSettings` hand-copied field-by-field** into the options pipeline inside `AddSentinelCore` — adding a settings property requires editing the copy loop too (silent drift). Settings also bundle unrelated concerns (SQL connection string, tier models, RAG options, trace flags) into one mutable bag passed as a live object. | `SentinelCoreServiceExtensions.cs:84–99` |
| M5 | **Two mutation paths into case lifecycle.** UI ViewModels (`CaseListViewModel`, `CaseDetailViewModel`, `CoreChatViewModel`) call `ICaseFlowEngine` directly, bypassing orchestration. The engine's doc says "No agent, orchestrator, or host may mutate case status directly", yet the host owns a second lifecycle path with its own concurrency semantics. | `CaseListViewModel.cs:282`, `CaseDetailViewModel.cs:41`, doc header in `CaseFlowEngine.cs:117–121` |
| M6 | **Test suite is hollow.** `AgentFactoryTests` bodies are fully commented out (always-green tests); both MSTest **and** xunit are referenced in one project; the pattern-lock claims tests enforce PL-9 (`ExecutorTemplateLockTests`) — no such file exists; no tests target the executor/agent core. The critical 14-executor graph has zero coverage. Test debris (`TestResults/`, `model-configuration.json`) committed alongside. | `AgentFactoryTests.cs:29–56`, `SentinelCore.Tests.csproj:22,27` |
| M7 | **Repo hygiene.** `openssl.exe` binary committed in the Security module (docs call it a leftover); a ~700KB chat **transcript with test-failure output** (`SafetyEngineTranscript.jsonl`) committed inside the SafetyEngine source folder; empty placeholder `DependencyInjection/` folder in Contracts kept alive by `<Folder Include>` items. | `SentinelCoreSecurity/openssl.exe`; `SafetyEngine/SafetyEngineTranscript.jsonl`; `Contracts.csproj:22` |
| M8 | **Dead/scaffold weight.** `SentinelCoreService` is a .NET Framework 4.8 non-SDK project with `BuildDependency`s on net10 projects it cannot reference — a build-order-only artifact excluded from every build. `OrchestrationControl` is a pure pass-through wrapper around `IOrchestration` (resolved eagerly at singleton construction). `OrchestrationFactory` silently defaults unknown enum values to TheCore — masking configuration errors. | `SentinelCoreService.csproj`, `OrchestrationControl.cs:29–52`, `OrchestrationFactory.cs:88–95` |
| M9 | **Prompt/spec content embedded in code.** Full task-hypothesis prompts as string literals inside `TheCoreExec`; role instructions in `AgentInstructionConstants`; 13 safety rules instantiated statically inside `SafetyExecutor`. Changing a prompt or safety posture = recompile. | `TheCoreExec.cs:53–90`, `SafetyExecutor.cs:39–76` |
| M10 | **Config discovery is library-owned.** `AddSentinelCore` computes `%APPDATA%` paths, reads `SENTINEL_MCP_REGISTRY_PATH`, and derives a DPAPI token-cache path — composition-root concerns inside the library. | `SentinelCoreServiceExtensions.cs:164–170` |

### Low

- `App.GetService<T>()` exposes the raw container as a UI-wide service locator (`App.xaml.cs:210`).
- Stringly-typed navigation keys (`typeof(ViewModel).FullName!` as page key).
- Tracer built manually instead of `OpenTelemetry.Extensions.Hosting` (`App.xaml.cs:231–240`).
- `AddDbContextFactory` deliberately registers both factory and scoped context — documented, but still a captive-dependency trap sitting in the container for any future accidental injection.
- Executor naming inconsistent (`…Exec`, `…Executor`, bare nouns: `EvaluationExec`, `TerminateWorkflow`, `CriticalAlert`, `PersistEvidence`).
- `Directory.Build.props` fails to centralize `Nullable`/`ImplicitUsings`, which are repeated (inconsistently typed — `true` vs `enable`) across six csproj files.

---

## 3. Design Options and Tradeoffs

**Option A — Incremental cleanup (keep layering, fix seams in place)**
Move `ICaseFlowEngine` + DTOs into Contracts, invert the entity-mapping direction, register executors in DI, kill `IServiceProvider` in workflow constructors. Lowest delivery risk; preserves every locked pattern.

**Option B — Ports & adapters re-alignment (recommended target)**
Treat CaseFlowEngine as an *adapter* behind Contracts-declared ports; make Orchestrations depend **only** on Contracts (drop its CaseFlowEngine references); entity → DTO mapping moves *into* the adapter; executors become DI-registered, injected explicitly; workflow composition becomes a declarative graph factory. Bigger refactor, but collapses C1/C2/C4 simultaneously and makes the optional-persistence story real (today "optional" is only true because null-objects half-work).

**Option C — Consolidate around MAF-native structure**
Fold executors into the MAF workflow graph API fully (no local `Executor` wrapper taxonomy), externalize prompts/rules to config, and let the DI container own everything. Highest coherence, largest rewrite, most exposure to MAF's own churn ("MAF is still under development").

---

## 4. Recommended Structure and Pattern

Option **B**, staged:

1. **Ports move up:** `ICaseFlowEngine`, `CaseStatus` (into `CaseFlow` sub-namespace, not `Cfe`), `IEvidenceStore`, `ISignalRepository`, and the domain DTOs all live in `SentinelCore.Contracts`. Contracts sheds `SqlClient`, `Microsoft.Agents.AI.Abstractions` (define a local tool-model interface or accept the AI-abstraction package as an explicit, documented contract dependency — decide one, don't straddle), and the `SystemReporter` implementation moves to Orchestrations.
2. **Adapters own mapping:** entity↔DTO converters move into `SentinelCore.CaseFlowEngine`; no DBEntities type appears outside the project. The `Signal`/`SignalEntity` dual-overload duality is resolved to DTO-only across the boundary.
3. **DI-registered executors, explicit graph:** delete `ExecutorFactory`; register each executor; `TheCoreWorkflow` receives `IReadOnlyList`/named executor dependencies by constructor. Null-object defaults are replaced by an explicit `AddSentinelCoreCoreOnly()` vs host `AddCaseFlowEngine()` — and the null-objects get fixed to actually never throw.
4. **Immutability at the seam:** `AgentPresetDefinition` becomes fully init-only; call-site overrides are `with`-clones.
5. **One result type:** a single `ToolResult` (with `ErrorCategory`/`ErrorCode`) in Contracts, used by in-process tools; the generic variant is folded in or deleted.
6. **Prompts/rules externalized:** safety rules and role/task instructions become registered `ISafetyRule` services and JSON/persona files — the executor graph stops hardcoding behavior.

## 5. Architectural Transition Strategy

- **Phase 1 (behavior-preserving):** fix null-objects, delete dead public members, remove `openssl.exe`/transcripts, make `Directory.Build.props` centralize nullable/usings/versions, remove `Builds` targets. Zero risk.
- **Phase 2 (seam inversion):** declare ports in Contracts, move mapping into CFE, switch Orchestrations' references. Compile-time guided; large but mechanical.
- **Phase 3 (composition hygiene):** DI-registered executors, `IServiceProvider` removal from workflows, immutable presets. Requires re-verifying the 14-executor graph.
- **Phase 4 (externalization/consolidation):** prompts, rules, single `ToolResult`, stub-or-cut decisions for RAG/PatternMemory (`[Experimental]` surfaces should be *unreachable* from the production graph until real — today they are wired and silently no-op).

## 6. Risks and Mitigations

| Risk | Mitigation |
| ------ | ----------- |
| MAF API churn during refactor | Phase 2/3 behind the new ports; only contracts touch MAF types |
| Hidden behavior dependency on `ActivatorUtilities` construction (optional ctor params, property init) | Register explicit DI factories per executor first, then delete `ExecutorFactory` |
| UI regression from VM/engine decoupling (M5) | Introduce a read-model facade for UI queries; UI writes go through one application service |
| Tests don't protect the refactor (M6) | Contract tests on ports + a graph-composition test must land **before** Phase 2 |
| Drift between the two `Directory.Packages.props` (Security module) | Merge the module into the main repo's central versions or generate from one |

## 7. Validation and Tests

- **Architecture tests (real, not aspirational):** an assembly-reference test asserting Orchestrations does not reference CaseFlowEngine; Contracts references no vendor packages; no `DBEntities` type appears outside CFE.
- **Null-object contract tests:** every `Null*` member returns defaults and never throws (would have caught the `NotImplementedException`s immediately).
- **Graph composition test:** build `TheCoreWorkflow` from DI with stub executors; assert the 15-node `ExecutorCollection` resolves and every `[YieldsOutput]` type matches its edge target.
- **Preset immutability test:** concurrent `CreateAgentAsync` with name overrides must not leak overrides into other agents (would have caught H1).
- **Stub-detection test:** fail CI if any executor reachable from the entry gate contains TODO-only bodies returning pass-through values (`PersistEvidence`, `CaseUpdateExecutor` today).

**Bottom line:** the intended architecture is sound; the executed architecture leaks at exactly one decisive point (the CFE seam) and circumvents itself at exactly one decisive point (service-located executor construction). Fixing those two, plus deleting the duplicate types and the always-green tests, would move this from documented-architecture to actual-architecture.
