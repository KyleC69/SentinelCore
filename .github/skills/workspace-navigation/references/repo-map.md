# SentinelCore — Full Repository Map

Detailed file-level map of every project in the repository. Use this as a reference when you need to find a specific file or understand what's in each folder.

## Project: SentinelCore.Contracts

**Path**: `projects/SentinelCore.Contracts/`
**Type**: Class library | **TFM**: `net10.0` | **Root namespace**: `SentinelCore`
**Project refs**: None (zero dependencies — pure DTOs and interfaces)

```
SentinelCore.Contracts/
├── Abstractions/
│   ├── IEvidenceStore.cs              # Evidence storage interface
│   ├── IPatternMemoryStore.cs         # Pattern memory storage interface
│   ├── ISignalRepository.cs           # Signal repository interface
│   ├── ISystemReporter.cs             # Aggregates output to loggers + events
│   ├── PatternMemoryResult.cs         # Pattern memory query result DTO
│   └── SystemReporter.cs              # ISystemReporter impl — aggregates output to loggers + events
├── CaseFlow/
│   ├── Case.cs                        # Case domain model
│   ├── CaseStatus.cs                  # Enum: case lifecycle states
│   ├── Evidence.cs                    # Evidence domain model
│   ├── InvestigationObjective.cs      # Investigation plan model
│   ├── InvestigationOutcome.cs        # Investigation outcome model
│   ├── PatternMemory.cs               # Pattern memory model
│   ├── Remediation.cs                 # Remediation model
│   └── Signal.cs                      # Signal domain model (triggers investigations)
├── Contracts/
│   ├── ModelProfile.cs                # Model config (endpoint, modelId, provider, temperature, etc.)
│   ├── ModelTier.cs                   # Enum: model tiers (per-role defaults)
│   ├── OrchestrationType.cs           # Enum: orchestration types
│   └── SentinelCoreSettings.cs        # Main settings class (top-level config)
├── DependencyInjection/               # (empty — reserved)
├── Events/
│   ├── ActivityType.cs                # Enum for event types
│   ├── ISentinelCoreEvents.cs         # Central event hub interface
│   ├── OrchestrationOutputEventArgs.cs
│   ├── SentinelCoreEvents.cs          # Default impl — multicast events
│   ├── SentinelOutputEventArgs.cs
│   └── WorkflowOutputEventArgs.cs
└── Mcp/
    ├── IMcpServerRegistry.cs          # MCP server registry interface
    ├── IMcpServerRegistryStore.cs     # Registry persistence interface
    ├── McpOAuthSettings.cs            # OAuth settings DTO
    ├── McpServerDefinition.cs         # MCP server definition DTO
    ├── McpServerInfo.cs               # MCP server info DTO
    ├── McpServerStatus.cs             # Enum: server status
    └── McpServerTransportType.cs      # Enum: transport types
```

> **Note:** `IErrorReporter.cs` and `Throw.cs` (previously listed in `Abstractions\`) no longer exist there. A `Throw.cs` helper now lives in `CaseFlowEngine\Infrastructure\`.

## Project: SentinelCore.CaseFlowEngine

**Path**: `projects/SentinelCore.CaseFlowEngine/`
**Type**: Class library | **TFM**: `net10.0-windows` | **Root namespace**: `SentinelCore`
**Project refs**: → Contracts

```
SentinelCore.CaseFlowEngine/
├── .env                               # Local dev environment variables (DB connection)
├── CaseEngine/                        # (empty — reserved)
├── CaseFlow/
│   ├── CaseFlowEngine.cs              # ICaseFlowEngine impl + interface definition
│   └── CaseFlowException.cs           # Case flow exceptions
├── DBEntities/                        # EF Core entities & partials for SentinelCoreDBContext
│   ├── ArtifactEntity.cs
│   ├── CaseEntity.cs
│   ├── DbContextExtensions.cs         # SqlQueryAsync helper
│   ├── EvidenceEntity.cs
│   ├── InvestigationObjective.cs
│   ├── ISentinelCoreDBContextProcedures.cs
│   ├── PatternMemoryEntity.cs
│   ├── RemediationEntity.cs
│   ├── SentinelCoreDBContext.cs       # EF Core DbContext (case-flow DB: SENTINEL_CORE)
│   ├── SentinelCoreDBContext.Functions.cs
│   ├── SentinelCoreDBContextProcedures.cs
│   └── SignalEntity.cs
├── Infrastructure/
│   ├── DependencyInjection/
│   │   └── CaseFlowEngineBuilderExtensions.cs  # CFE DI registration extensions
│   └── Throw.cs                       # Throw.IfNull null-check helper
├── Migrations/                        # EF Core migrations (initial → pattern-mem update)
├── Persistence/                       # RAG/pattern-memory persistence + stores
│   ├── CaseEntityExtensions.cs
│   ├── DesignTimeSentinelCoreDBContextFactory.cs  # Design-time DB context factory
│   ├── EvidenceMappingExtensions.cs   # entity↔domain mapping
│   ├── EvidenceStore.cs               # IEvidenceStore impl (EF Core)
│   ├── NullPersistenceServices.cs     # No-op store implementations (never throw)
│   ├── PatternMemoryMappingExtensions.cs
│   ├── PatternMemoryStore.cs          # IPatternMemoryStore impl (EF Core)
│   ├── RemediationEntityExtensions.cs
│   ├── SentinelRAGDBContext.Functions.cs
│   ├── SentinelRAGDBContextProcedures.cs
│   ├── SignalMappingExtensions.cs
│   └── SignalRepository.cs            # ISignalRepository impl (EF Core)
├── dab-build.cmd                      # Data API builder build script
├── dab-options.json                   # Data API builder config
├── efpt.config.json                   # EF Core Power Tools config
└── migration.sql                      # SQL migration script
```

## Project: SentinelCore.Orchestrations

**Path**: `projects/SentinelCore.Orchestrations/`
**Type**: Class library | **TFM**: `net10.0-windows` | **Root namespace**: `SentinelCore`
**Project refs**: → Contracts, CaseFlowEngine, SentinelCoreSecurity (out-of-tree)
**Key packages**: Microsoft.Agents.AI (MAF), Microsoft.Agents.AI.Workflows, OllamaSharp 5+, ModelContextProtocol

This is the **core engine project** — contains all agents, workflows, safety, RAG, MCP connections, and DI.

```
SentinelCore.Orchestrations/
├── GlobalUsings.cs
├── Abstractions/
│   ├── AgentToolGroup.cs              # Tool group definition
│   ├── AgentToolGroupCatalog.cs       # Tool-group registry (sc_<groupprefix>_ scheme)
│   ├── AgentToolSelection.cs          # Tool selection DTO
│   ├── IAgentPersona.cs               # Persona interface (Name, Description, Instructions)
│   ├── IAgentPresetProvider.cs        # Preset provider interface
│   ├── IChatClientFactory.cs          # Chat client factory interface
│   ├── IOrchestration.cs              # Orchestration interface (BuildWorkflow, ExecuteAsync)
│   ├── IOrchestrationControl.cs       # Control interface for investigations
│   └── ISentinelAgentFactory.cs       # THE agent factory contract — all agents via presets (PL-3)
├── Agents/
│   ├── AgentInstructionConstants.cs   # Static role instruction texts
│   ├── ModelResolver.cs               # Resolves ModelProfile per tier/role
│   ├── SentinelAgentFactory.cs        # ISentinelAgentFactory — THE agent construction pipeline
│   ├── SentinelChatClientFactory.cs   # IChatClient per provider (Ollama)
│   ├── AgentPresets/
│   │   ├── AgentPreset.cs             # Preset DTO/registry types
│   │   └── AgentPresetDefinition.cs   # Immutable preset record (model, tier, tools, middleware)
│   ├── Core/
│   │   └── Tools/
│   │       ├── AgentSpawnTools.cs     # Core tools for spawning worker agents
│   │       ├── CaseEngineTools.cs     # Core tools for case engine operations
│   │       ├── MsLearnMcpTools.cs     # MCP tool — Microsoft Learn server (temporary, ADR-007)
│   │       ├── README.md              # Core tools documentation
│   │       └── ToolResult.cs          # Core tool result type
│   └── Middleware/
│       ├── IPatternMatcher.cs         # Pattern matching interface
│       ├── PatternContext.cs          # Pattern context DTO
│       ├── PatternMemoryInjector.cs   # RAG pattern memory injection (AIContextProvider)
│       └── SemanticPatternMatcher.cs  # Semantic pattern matching
├── Application/
│   ├── AISystemsInitialization.cs     # AI subsystem bootstrapping
│   ├── OrchestrationControl.cs        # IOrchestrationControl impl — primary entry point
│   ├── OrchestrationFactory.cs        # IOrchestrationFactory — creates orchestration by type
│   └── WorkflowExecutionOutput.cs     # Output DTO
├── Exceptions/
│   ├── SafetyEngineInternalException.cs
│   ├── SafetyImmediateBlockException.cs
│   ├── SentinelCaseEngineException.cs
│   ├── SentinelCoreAgentException.cs
│   ├── SentinelCoreExecutionException.cs
│   ├── SentinelCoreModelException.cs
│   ├── SentinelCorePlatformException.cs
│   ├── SentinelCoreWorkflowException.cs
│   └── SentinelOrchestrationException.cs
├── Infrastructure/
│   ├── Attributes.cs                  # [YieldsOutput], [MessageHandler], etc. (PL-9)
│   ├── CustomMAFFormatter.cs          # Log/message formatting helpers
│   ├── Diagnostics/                   # Diagnostic helpers
│   ├── InvestigationObjective.cs      # DTO
│   ├── JsonLoggerProvider.cs          # Custom JSON file logger
│   ├── LogFileFormatter.cs            # Log file format
│   └── DependencyInjection/
│       ├── ExecutorFactory.cs         # Executor factory
│       ├── ExecutorRegistration.cs    # Registers all executors in DI
│       └── SentinelCoreServiceExtensions.cs  # AddSentinelCore() — THE DI entry point
├── Mcp/                               # MCP client infrastructure (connections to MCP servers)
│   ├── DpapiTokenCache.cs             # DPAPI-protected token cache
│   ├── IMcpConnectionFactory.cs
│   ├── JsonFileMcpServerRegistryStore.cs  # IMcpServerRegistryStore impl (JSON file)
│   ├── LoopbackOAuthCallbackHandler.cs
│   ├── McpConnectionFactory.cs        # Creates MCP client connections
│   ├── McpServerRegistry.cs           # IMcpServerRegistry impl
│   ├── McpToolTimeoutFunction.cs      # Timeout wrapper for MCP tools
│   └── README.md
├── Models/
│   ├── ChatMessageExtensions.cs       # Provenance stamping extension methods
│   ├── CoreSwitchDecision.cs          # CoreSwitch routing decision DTO
│   ├── Ledger.cs                      # EvidenceItem, investigation step DTOs
│   └── MessageSourceType.cs           # Enum: message source types
├── Orchestrations/
│   ├── ApprovalBasedManager.cs        # RoundRobinGroupChatManager subclass (approval gate)
│   ├── MagenticDefaultPrompts.cs      # Default prompts for magentic workflow
│   └── MagenticPromptOverrides.cs     # Prompt override hooks
├── Personas/
│   └── PersonaRegistry.cs             # Static factory: agent personas
├── Providers/
│   └── AdvancedInMemoryChatHistoryProvider.cs  # Chat history provider
├── Rag/
│   ├── IRagSearchService.cs
│   ├── RagContextInjector.cs          # Injects RAG context into agent run
│   ├── RagMiddlewarePipeline.cs       # RAG pipeline steps
│   ├── RagSearchOptions.cs
│   ├── RagSearchService.cs            # IRagSearchService impl
│   └── RagSearchTool.cs               # Exposes RAG search as an AITool
├── SafetyEngine/
│   ├── ISafetyRule.cs                 # Safety rule interface
│   ├── SafetyEngineAgent.cs           # Safety middleware agent (intercepts prompts)
│   ├── SafetyEngineAgentBuilderExtensions.cs  # UseSafetyEngine() on AIAgentBuilder
│   ├── SafetyEngineOptions.cs
│   ├── SafetyEvaluationContext.cs
│   ├── SafetyEvaluationResult.cs
│   ├── SafetyRuleEngine.cs            # Rule aggregation
│   ├── SafetyRuleResult.cs
│   ├── SafetySeverity.cs
│   ├── SafetyTagger.cs                # Tags messages with safety metadata
│   ├── BlockPhrases.cs                # Static block phrase list
│   ├── SafetyEngineTranscript.jsonl   # Design transcript artifact (large; do not search by default)
│   └── Rules/                         # 18 safety rule implementations
├── Services/
│   ├── IWorkflowEventProcessor.cs
│   └── WorkflowEventProcessor.cs      # Processes workflow events
└── Workflows/
    ├── ConcurrentGroupTeam.cs         # IOrchestration — concurrent group team
    ├── CustomGroupWorkflow.cs         # IOrchestration — custom group workflow (investigation team)
    ├── NextStep.cs                    # Routing enum (RedAlert, Investigate, MoreInformationRequired,
    │                                  #   EscalateToHumanOperator, DirectAnswer)
    ├── TheCoreWorkflow.cs             # THE main workflow — classifies & routes signals
    ├── WorkflowBase.cs                # Base class for workflows
    ├── WorkFlowStateKeys.cs           # Shared state key constants
    ├── workflow.dot                   # Workflow diagram
    ├── Helpers/
    │   └── DetectionBoolResult.cs     # Detection result DTO
    └── Executors/                     # 23 files — executors, envelopes, and support types (see executors map below)
```

> **Notes:**
> - `Tools/` no longer exists under Orchestrations — all Windows diagnostic tools moved to `SentinelCore.MCPTools` (delivered via MCP server).
> - `AgentRole.cs`, `AgentProfile.cs`, `AgentProfileBuilder.cs`, `AgentMiddlewarePipeline.cs`, `TheCoreRunner.cs`, `ModelNoiseSafety.cs`, `SentinelChatClientBuilderExtensions.cs`, `CaseGenerator.cs`, and `ToolRegistry.cs` have been removed or moved: construction now goes through `AgentPresetDefinition` + `SentinelAgentFactory` per PL-3.
> - `MagneticOrchestration.cs` no longer exists — the Manager runs a MAF `MagenticWorkflowBuilder` group (see `TheCoreWorkflow.cs`) with `ApprovalBasedManager`.
> - `CoreRoutingDecision.cs` was replaced by `NextStep.cs` and `Models\CoreSwitchDecision.cs`.

## Project: SentinelCore.MCPTools

**Path**: `projects/SentinelCore.MCPTools/`
**Type**: MCP server console app (Exe, PackAsTool) | **TFM**: `net10.0` | **Root namespace**: `SentinelCoreMCP`
**Note**: csproj is `SentinelCore.MCP.csproj` (project folder name differs from csproj name). Packaged and published as `SentinelCoreMCP` NuGet tool; 141 read-only `sc_*` tools across 11 groups, stdio transport, never throws (returns `ToolResult`).

```
SentinelCore.MCPTools/
├── Docs/
│   ├── ToolingComponent.md            # Tool naming scheme (canonical, per ADR-006)
│   ├── MCPServerToolStandardSpecification.md
│   └── ToolAuditFindings-2026-11-18.md
├── .mcp/
│   └── server.json                    # MCP server manifest
├── Tools/                             # All Windows diagnostic/scouting tools (one file per domain)
│   ├── McpToolFactory.cs              # Tool registration/factory
│   ├── MCPToolRegistration.cs         # Tool catalogue registration
│   ├── ToolCollection.cs              # Tool collection type
│   ├── ToolResult.cs                  # Universal result object — every tool returns ToolResult
│   ├── InputValidator.cs
│   ├── Helpers.cs
│   ├── RegistryHelper.cs
│   ├── QueryCimInstance.cs
│   ├── PowerShellResult.cs
│   ├── Interop/                       # P/Invoke + COM interop helpers
│   └── ... (90+ *ReadTool.cs / ExtendedReadTool.cs files: AccessChk, ActiveDirectory, AppLocker,
│            Autoruns, Bitlocker, CertificateStore, CoreInfo, Defender, EventLog, FileSystem,
│            Firewall, Handle, HyperV, ListDlls, LogonSessions, NtfsInfo, PendMoves, PowerShell,
│            ProcDump, ProcessExplorer, PsInfo, PsList, PsLogList, PsService, Registry, RegDelNull,
│            Sessions, ShareEnum, SigCheck, System, TcpView, WmiQuery, and more)
├── Utils/
├── Program.cs                         # Stdio MCP server host
└── SentinelCore.MCP.csproj            # PackAsTool / PackageType=McpServer / self-contained single file
```

## Project: SentinelCore.UI

**Path**: `projects/SentinelCore.UI/`
**Type**: WPF app (WinExe) — active composition root | **TFM**: `net10.0-windows`
**Project refs**: → Contracts, CaseFlowEngine, Orchestrations, SentinelCoreSecurity (out-of-tree)

```
SentinelCore.UI/
├── App.xaml / App.xaml.cs             # Startup, DI composition
├── MainWindow.xaml / .cs              # Shell window
├── Models/                            # UI models
├── Converters/                        # Value converters
├── Services/                          # UI services (navigation, stores, dialogs)
│   ├── NavigationService.cs / INavigationService.cs
│   ├── ViewLocator.cs / IViewLocator.cs
│   ├── StartupStateService.cs
│   ├── FileModelConfigStore.cs / FileLoggerServiceStore.cs
│   ├── IModelConfigStore.cs / IModelConfigGate.cs / ModelConfigDocument.cs
│   ├── IClipboardService.cs / WpfClipboardService.cs
│   ├── IDialogService.cs / WpfDialogService.cs
│   ├── IDispatcherService.cs / WpfDispatcherService.cs
│   ├── IFolderBrowserService.cs / WpfFolderBrowserService.cs
│   ├── LogViewerLoggerProvider.cs
│   ├── INavigationAware.cs
│   └── SentinelCoreUIServiceExtensions.cs   # Host-side DI registration
├── ViewModels/
│   ├── CoreChatViewModel.cs           # Main chat VM — uses IOrchestrationControl (+ provenance identity)
│   ├── CaseListViewModel.cs
│   ├── CaseDetailViewModel.cs
│   ├── CreateCaseViewModel.cs
│   ├── McpServersViewModel.cs         # MCP server registry management
│   └── ModelConfigViewModel.cs
└── Views/
    ├── CoreChatPage.xaml / .cs        # Main chat UI
    ├── CaseListPage.xaml / .cs
    ├── CaseDetailPage.xaml / .cs
    ├── CreateCasePage.xaml / .cs
    ├── McpServersPage.xaml / .cs
    ├── ModelConfigPage.xaml / .cs
    ├── ConfirmationDialog.xaml / .cs
    └── LogViewerWindow.xaml / .cs
```

## Project: SentinelCoreService

**Path**: `projects/SentinelCoreService/`
**Type**: Windows Service scaffold | **TFM**: `net10.0-windows` | **Note**: Excluded from normal solution build (per `SolutionArchitecture.md`)
**Project refs**: → Contracts, CaseFlowEngine, Orchestrations

```
SentinelCoreService/
├── App.config
├── Program.cs                         # Service entry point
├── SentinelCoreService.cs             # Service implementation
├── SentinelCoreService.Designer.cs
├── ProjectInstaller.cs / .Designer.cs / .resx   # Service installer
└── Windows_Service_Database_Timer.txt # Design notes (timer-driven DB work)
```

## Project: SentinelCore.Security (out-of-tree)

**Path**: `F:\Solutions\SentinelCoreSecurity\` — sibling of the workspace root; **solution member** of `SentinelCore.slnx` via `../SentinelCoreSecurity/SentinelCore.Security.csproj`
**Type**: Class library | **TFM**: `net10.0` | **Root namespace**: `SentinelCore.Security`
**Project refs**: → Contracts only (module boundary per PL-1; see ADR-008)

```
SentinelCoreSecurity/
├── Cryptography/                      # HMAC-SHA256 signing helpers
├── Identity/                          # Identity registry (stub)
├── Quarantine/                        # Quarantine containment
├── Infrastructure/
│   └── DependencyInjection/           # SentinelSecurityServiceExtensions
├── ISentinelProvenanceService.cs      # Provenance service interface
├── SentinelProvenanceService.cs       # Provenance service impl (HMAC-SHA256, fail-closed)
├── Provenance.cs                      # Provenance stamping
├── SentinelSecurityOptions.cs         # Options (SENTINEL_KEY env var)
└── SentinelCore.Security.csproj
```

## Executors map (Orchestrations\Workflows\Executors\)

```
Executors/
├── CaseUpdateExecutor.cs
├── CriticalAlert.cs
├── DirectAnswerExecutor.cs
├── EscalatedExecutor.cs
├── EvaluationExec.cs
├── ExecutorEnvelope.cs               # Executor wrapper/envelope (PL-9 support)
├── ExecutorTemplate.cs               # Reference template for new executors
├── GenerateInvestigationObjective.cs
├── HarnessTest.cs
├── HumanOperatorExecutor.cs
├── MessageSourceAttribution.cs
├── MoreInformationExecutor.cs
├── NewCaseExecutor.cs
├── PatternCheckExecutor.cs
├── PersistEvidence.cs
├── ProvenanceGateExecutor.cs         # Fail-closed provenance check (ADR-008)
├── SafetyExecutor.cs
├── SafetyReviewExecutor.cs
├── SignalClassifierExecutor.cs       # Signal classification
├── SubWorkflowExecutor.cs
├── TerminateWorkflow.cs
├── TheCoreExec.cs                    # Core agent executor — persistent session
└── TheCoreSwitchExec.cs              # Core switch executor (provenance-aware routing)
```

## Documentation Files

All in `docs/`:

| File | Content |
| ------ | --------- |
| `SolutionArchitecture.md` | Full architecture overview, dependency graph, layer architecture |
| `ProjectTerminology.md` | Term definitions (same as `.github/instructions/`) |
| `OrchestrationComponent.md` | Orchestration design |
| `CaseFlowEngineComponent.md` | CFE design |
| `ContractsComponent.md` | Contracts layer |
| `MemoryLayerComponent.md` | Pattern memory |
| `SafetyRailsComponent.md` | Safety engine |
| `Safety-Valve-Overview.md` | Safety valve overview |
| `ToolingComponent.md` | Tooling design (tool groups, prefix scheme) |
| `DynamicAgentsComponent.md` | Dynamic/composite agents |
| `PersistenceComponent.md` | Persistence layer |
| `CaseManagement.md` | Case management |
| `ExampleCaseSchema.md` | Example case schema |
| `Documentation-Manifest.md` | Doc manifest |
| `Proposed-SafetyEngine-Group-Collaboration.md` | Proposed safety engine design |
| `SafetyEngineComponent.md` | Safety engine component design |
| `SafetyEngine-Implementation-Checklist.md` | Safety engine implementation checklist |
| `SafetyEngine-Transcript-Design-Plan.md` | Safety engine transcript design plan |
| `SecurityComponent.md` | SentinelCoreSecurity provenance module design |
| `McpServerRegistryComponent.md` | MCP server registry design |
| `ADR/` | Architecture Decision Records (001–008) |
| `ADR/zzz-control-probe-do-not-create.md` | Control probe — do not create/copy |

> **Note:** `docs/user/` no longer exists (`ConfigurationAndCustomization.md` removed). The old `workflow.dot`/`workflow.mermaid` diagrams live at `projects/SentinelCore.Orchestrations/Workflows/workflow.dot` only.

## .github/ Structure

```
.github/
├── copilot-instructions.md            # Copilot instructions (conventions, design principles, MAF types)
├── instructions/
│   ├── drift-prevention.instructions.md   # Drift prevention rules (applyTo: **)
│   ├── ProjectTerminology.instructions.md # Canonical terminology (applyTo: **)
│   └── ArchitectureGate.md                # Pre-code architecture gate checklist
├── skills/
│   ├── workspace-navigation/          # THIS skill (SKILL.md + references/)
│   └── create-adr/                    # ADR creation skill (SKILL.md)
└── agents/
    ├── WPF UI Agent.agent.md          # WPF UI specialist agent
    ├── integration-architect.agent.md # Read-only planning agent
    └── regression-Test.agent.md       # Regression testing agent
```

> **Note:** `.github/prompts/` is empty — the create-adr prompt referenced by `pattern-lock.md` and `create-adr/SKILL.md` does not currently exist.
