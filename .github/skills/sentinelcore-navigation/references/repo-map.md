# SentinelCore — Full Repository Map

Detailed file-level map of every project in the repository. Use this as a reference when you need to find a specific file or understand what's in each folder.

## Project: SentinelCore.Contracts

**Path**: `projects/SentinelCore.Contracts/`
**Type**: Class library | **TFM**: `net10.0` | **Root namespace**: `SentinelCore`
**Project refs**: None (zero dependencies — pure DTOs and interfaces)

```
SentinelCore.Contracts/
├── Abstractions/
│   ├── IErrorReporter.cs              # Defines ISystemReporter interface (aggregates output)
│   ├── IEvidenceStore.cs              # Evidence storage interface
│   ├── IPatternMemoryStore.cs         # Pattern memory storage interface
│   ├── ISignalRepository.cs           # Signal repository interface
│   ├── PatternMemoryResult.cs         # Pattern memory query result DTO
│   ├── SystemReporter.cs              # ISystemReporter impl — aggregates output to loggers + events
│   └── Throw.cs                       # Null-check helper (Throw.IfNull)
├── CaseEngine/                        # (empty — reserved)
├── CaseFlow/
│   ├── Case.cs                        # Case domain model
│   ├── Evidence.cs                    # Evidence domain model
│   ├── ICaseFlowEngine.cs             # Case engine interface (stub — full impl in CaseFlowEngine/Cfe)
│   ├── InvestigationPlan.cs           # Investigation plan model
│   ├── InvestigationPlanStep.cs       # Single step in an investigation plan
│   ├── Resolution.cs                  # Case resolution model
│   └── Signal.cs                      # Signal domain model (triggers investigations)
├── Cfe/
│   └── CaseStatus.cs                  # Enum: case lifecycle states
├── Contracts/
│   ├── ModelProfile.cs                # Model config (endpoint, modelId, provider, temperature, etc.)
│   ├── OrchestrationType.cs           # Enum: TheCore, CustomGroup
│   └── SentinelCoreSettings.cs        # Main settings class (top-level config)
├── DependencyInjection/
│   └── ISentinelCoreBuilder.cs        # Builder interface for module configuration
└── Events/
    ├── ActivityType.cs                # Enum for event types
    ├── ISentinelCoreEvents.cs         # Central event hub interface
    ├── OrchestrationOutputEventArgs.cs
    ├── SentinelCoreEvents.cs          # Default impl — multicast events
    └── SentinelOutputEventArgs.cs
```

## Project: SentinelCore.CaseFlowEngine

**Path**: `projects/SentinelCore.CaseFlowEngine/`
**Type**: Class library | **TFM**: `net10.0-windows` | **Root namespace**: `SentinelCore`
**Project refs**: → Contracts

```
SentinelCore.CaseFlowEngine/
├── CaseEngine/                        # (empty — reserved)
├── Cfe/
│   ├── CaseFlowEngine.cs              # ICaseFlowEngine impl + full interface definition
│   └── PatternMemory.cs               # Pattern memory logic
├── Infrastructure/
│   ├── DependencyInjection/
│   │   └── CaseFlowEngineBuilderExtensions.cs  # CFE DI registration extensions
│   └── Persistence/
│       ├── DatabaseInitializer.cs     # DB initialization logic
│       ├── EvidenceStore.cs           # IEvidenceStore impl (EF Core)
│       ├── PatternMemoryStore.cs      # IPatternMemoryStore impl (EF Core)
│       └── SignalRepository.cs        # ISignalRepository impl (EF Core)
└── Persistence/                       # EF Core entities & mappings
    ├── CaseEntity.cs
    ├── CaseMappingExtensions.cs
    ├── DesignTimeSentinelCoreDBContextFactory.cs  # Design-time DB context factory
    ├── EvidenceEntity.cs
    ├── EvidenceMappingExtensions.cs
    ├── InvestigationPlanEntity.cs
    ├── InvestigationPlanMappingExtensions.cs
    ├── InvestigationPlanStepMappingExtensions.cs
    ├── InvestigationPlanStepsEntity.cs
    ├── PatternMemoryEntity.cs
    ├── PatternMemoryMappingExtensions.cs
    ├── ResolutionEntity.cs
    ├── ResolutionMappingExtensions.cs
    ├── SentinelCoreDbContext.cs       # EF Core DbContext
    └── SignalEntity.cs
```

## Project: SentinelCore.Orchestrations

**Path**: `projects/SentinelCore.Orchestrations/`
**Type**: Class library | **TFM**: `net10.0-windows` | **Root namespace**: `SentinelCore`
**Project refs**: → CaseFlowEngine, Contracts
**Key packages**: Microsoft.Agents.AI 1.17.0, Microsoft.Agents.AI.Workflows, OllamaSharp 5.4.30, ModelContextProtocol 2.2.0

This is the **core engine project** — contains all agents, tools, workflows, safety, and DI.

```
SentinelCore.Orchestrations/
├── GlobalUsings.cs
├── Abstractions/
│   ├── IAgentPersona.cs               # Persona interface (Name, Description, Instructions)
│   ├── IOrchestration.cs              # Orchestration interface (BuildWorkflow, ExecuteAsync)
│   └── IOrchestrationControl.cs       # Control interface for investigations
├── Agents/
│   ├── AgentMiddlewarePipeline.cs     # Predefined pipeline flags per role
│   ├── AgentProfile.cs                # Immutable spec record for constructing AIAgent
│   ├── AgentProfileBuilder.cs         # IAgentProfileBuilder — builds AgentProfile from AgentRole
│   ├── AgentRole.cs                   # Enum: Core, Manager, Utility
│   ├── CaseGenerator.cs               # ICaseGenerator — bulk case generation agent
│   ├── ModelNoiseSafety.cs            # DelegatingChatClient — sanitizes model output noise
│   ├── SentinelAgentFactory.cs        # ISentinelAgentFactory — THE agent construction pipeline
│   ├── SentinelChatClientBuilderExtensions.cs  # UseSentinelEvents on ChatClientBuilder
│   ├── SentinelChatClientFactory.cs   # Static factory: IChatClient per provider
│   ├── TheCoreRunner.cs               # Internal Executor<SignalHypothesis,string> for Core agent
│   ├── Core/
│   │   └── Tools/
│   │       ├── MSDocsMCPServerTool.cs # MCP tool — Microsoft Docs server
│   │       └── SimpleMcpClientTool.cs # MCP tool — simple MCP client
│   ├── Middleware/
│   │   ├── EventPublishingChatClient.cs     # DelegatingChatClient — publishes tool/text events
│   │   └── PatternMemoryInjector.cs   # MessageAIContextProvider — RAG pattern memory injection
│   └── Models/
│       └── Ledger.cs                  # EvidenceItem, InvestigationStep DTOs
├── Application/
│   ├── OrchestrationControl.cs        # IOrchestrationControl impl — primary entry point
│   ├── OrchestrationFactory.cs        # IOrchestrationFactory — creates orchestration by type
│   ├── SentinelWorkflowExecution.cs   # ISentinelWorkflowExecution — runs workflows
│   ├── ToolRegistry.cs                # Static registry — maps domain names → tool names (43 domains)
│   └── WorkflowExecutionResult.cs     # Result DTO
├── Exceptions/
│   ├── SentinelCaseEngineException.cs
│   ├── SentinelCoreModelException.cs
│   ├── SentinelCorePlatformException.cs
│   └── SentinelOrchestrationException.cs
├── Infrastructure/
│   ├── JsonLoggerProvider.cs          # Custom JSON file logger
│   └── DependencyInjection/
│       ├── ExecutorRegistration.cs    # Registers all executors in DI
│       ├── SentinelCoreBuilder.cs     # ISentinelCoreBuilder impl
│       └── SentinelCoreServiceExtensions.cs  # AddSentinelCore() — THE DI entry point
├── Orchestrations/
│   ├── ApprovalBasedManager.cs        # RoundRobinGroupChatManager subclass
│   └── MagneticOrchestration.cs       # IMagneticOrchestration — Manager workflow
├── Personas/
│   └── PersonaRegistry.cs             # Static factory: 33 personas (TheArchitect, TheEngineer, etc.)
├── SafetyEngine/
│   ├── ISafetyRule.cs                 # Safety rule interface
│   ├── SafetyEngineAgent.cs           # Safety middleware agent (intercepts prompts)
│   ├── SafetyEngineAgentBuilderExtensions.cs  # UseSafetyEngine() on AIAgentBuilder
│   ├── SafetyEngineOptions.cs
│   ├── SafetyEvaluationContext.cs
│   ├── SafetyEvaluationResult.cs
│   ├── SafetyRuleResult.cs
│   ├── SafetySeverity.cs
│   └── Rules/                         # 15 safety rule implementations
│       ├── BlocklistRule.cs
│       ├── CodeInjectionRule.cs
│       ├── CompositeRule.cs
│       ├── DataExfiltrationRule.cs
│       ├── EncodingEvasionRule.cs
│       ├── HarmfulContentRule.cs
│       ├── MaxLengthRule.cs
│       ├── PIIDetectionRule.cs
│       ├── PromptInjectionRule.cs
│       ├── RegexBlockRule.cs
│       ├── RepetitionAttackRule.cs
│       ├── RoleEscalationRule.cs
│       ├── SystemPromptExtractionRule.cs
│       ├── TokenLimitRule.cs
│       └── UrlBlockRule.cs
├── Tools/                             # 40+ Windows diagnostic AITools
│   ├── ToolResult.cs                  # Universal result object — every tool must return ToolResult
│   ├── Interop/
│   │   ├── PInvokeHelper.cs
│   │   └── SafeComObject.cs
│   ├── AccessibilityReadTool.cs
│   ├── AppLockerReadTool.cs
│   ├── AudioDeviceReadTool.cs
│   ├── AuditingTool.cs
│   ├── BatteryReadTool.cs
│   ├── BitlockerReadTool.cs
│   ├── BootConfigurationReadTool.cs
│   ├── BrowserConfigReadTool.cs
│   ├── CertificateStoreReadTool.cs
│   ├── CreateCaseTool.cs             # Tool that creates cases via ICaseFlowEngine
│   ├── CredentialsReadTool.cs
│   ├── DcomReadTool.cs
│   ├── DefenderReadTool.cs
│   ├── DisplayReadTool.cs
│   ├── DriversReadTool.cs
│   ├── EnvironmentVariablesReadTool.cs
│   ├── EventLogReadTool.cs
│   ├── FileSystemReadTool.cs
│   ├── FirewallReadTool.cs
│   ├── FontsReadTool.cs
│   ├── GroupPolicyReadTool.cs
│   ├── HyperVReadTool.cs
│   ├── InstalledAppsReadTool.cs
│   ├── LocalAccountsReadTool.cs
│   ├── NetworkReadTool.cs
│   ├── NotificationsReadTool.cs
│   ├── PerformanceReadTool.cs
│   ├── PnpDeviceReadTool.cs
│   ├── PowerSettingsReadTool.cs
│   ├── PrinterReadTool.cs
│   ├── ProcessesReadTool.cs
│   ├── ProxyReadTool.cs
│   ├── RegistryReadTool.cs
│   ├── RemoteDesktopReadTool.cs
│   ├── ScheduledTaskReadTool.cs
│   ├── SearchIndexingReadTool.cs
│   ├── SensorsReadTool.cs
│   ├── ShellExplorerReadTool.cs
│   ├── UacReadTool.cs
│   ├── VpnReadTool.cs
│   ├── WindowsServiceReadTool.cs
│   ├── WindowsUpdateReadTool.cs
│   ├── WirelessReadTool.cs
│   └── WmiQueryTool.cs
└── Workflows/
    ├── CoreRoutingDecision.cs         # NextStep enum (RedAlert, Investigate, MoreInformationRequired, etc.)
    ├── CustomGroup.cs                 # CustomGroupWorkflow — test harness (NOT real orchestration)
    ├── NullHandler.cs                 # Null-object pattern handler
    ├── SignalHypothesis.cs            # Shared workflow message DTO
    ├── TheCoreWorkflow.cs             # THE main workflow — classifies & routes signals
    ├── WorkflowBase.cs                # Base class for workflows
    ├── WorkflowMessage.cs
    └── Executors/                     # 20+ workflow executors
        ├── AgentExecutor.cs           # ClassifierAgentExec — runs classifier agent
        ├── AggregationExecutor.cs
        ├── AnalysisExecutor.cs
        ├── CaseGenExec.cs             # Case generation executor
        ├── CaseUpdateExecutor.cs
        ├── ClarificationExecutor.cs
        ├── CriticalAlert.cs
        ├── DirectAnswerExecutor.cs
        ├── EscalatedExecutor.cs
        ├── Executors.cs               # PersistTask and other simple executors
        ├── HumanOperatorExecutor.cs
        ├── InvestigationExecutor.cs   # Runs magnetic investigation sub-workflow
        ├── LoggingExecutor.cs
        ├── MoreInformationExecutor.cs
        ├── NewCaseExecutor.cs
        ├── PatternCheckExecutor.cs
        ├── PersistEvidence.cs
        ├── SafetyExecutor.cs
        ├── SubWorkflowExec.cs
        ├── TheCoreExec.cs             # Core agent executor — persistent session
        ├── VerifyEvidenceExecutor.cs
        ├── WhiteListExecutor.cs
        └── WorkFlowStateKeys.cs       # Shared state key constants
```

## Project: SentinelCoreHost (Phasing Out)

**Path**: `projects/SentinelCoreHost/`
**Type**: WPF app (WinExe) | **TFM**: `net10.0-windows`
**Project refs**: → Orchestrations, Contracts, CaseFlowEngine

```
SentinelCoreHost/
├── App.config                         # User settings (endpoint, model IDs, temperatures)
├── App.xaml / App.xaml.cs             # Startup — builds IHost, configures SentinelCoreSettings
├── MainWindow.xaml / .cs
├── TraceLogWindow.xaml / .cs
├── workflow.dot / workflow.mermaid    # Workflow diagrams
├── Services/
│   ├── ApplicationHostService.cs
│   ├── ApplicationInfoService.cs
│   ├── IdentityCacheService.cs
│   ├── PersistAndRestoreService.cs
│   ├── RuntimeAppSettingsService.cs
│   ├── ServiceCollectionRegistrationExtensions.cs  # Host DI modules
│   ├── SystemService.cs
│   └── TerminalLogForwarder.cs
├── ViewModels/
│   ├── AsyncRelayCommand.cs
│   ├── MainWindowViewModel.cs
│   └── RelayCommand.cs
└── Views/
    ├── LogWindow.xaml / .cs
    └── SettingsViewWindow.xaml / .cs
```

## Project: SentinelCoreAdmin (Active Host)

**Path**: `projects/SentinelCoreAdmin/`
**Type**: WPF app (WinExe) | **TFM**: `net10.0-windows10.0.19041.0`
**Project refs**: → Admin.Core, Contracts, Orchestrations, CaseFlowEngine

```
SentinelCoreAdmin/
├── appsettings.json                   # AppConfig (identity, MSAL, file paths)
├── App.xaml / App.xaml.cs             # Startup with ShutdownToken, toast activation
├── Activation/
│   └── ToastNotificationActivationHandler.cs
├── Contracts/                         # Service contracts (IActivationHandler, etc.)
├── Services/
│   ├── ApplicationHostService.cs
│   ├── ApplicationInfoService.cs
│   ├── IdentityCacheService.cs
│   ├── NavigationService.cs
│   ├── PageService.cs
│   ├── PersistAndRestoreService.cs
│   ├── RightPaneService.cs
│   ├── ServiceCollectionRegistrationExtensions.cs  # Admin DI modules
│   ├── SystemService.cs
│   ├── ThemeSelectorService.cs
│   ├── ToastNotificationsService.cs (+ .Samples.cs)
│   ├── UserDataService.cs
│   └── WindowManagerService.cs
├── ViewModels/
│   ├── CoreChatViewModel.cs           # Main chat VM — uses IOrchestrationControl
│   ├── LogInViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── ShellDialogViewModel.cs
│   ├── ShellViewModel.cs
│   ├── TraceLogViewModel.cs
│   └── UserViewModel.cs
└── Views/
    ├── CoreChatPage.xaml / .cs        # Main chat UI
    ├── LogInWindow.xaml / .cs
    ├── SettingsPage.xaml / .cs
    ├── ShellDialogWindow.xaml / .cs
    ├── ShellWindow.xaml / .cs
    └── TraceLogPage.xaml / .cs
```

## Project: SentinelCoreAdmin.Core

**Path**: `projects/SentinelCoreAdmin.Core/`
**Type**: Class library | **TFM**: `netstandard2.0`
**Project refs**: None

```
SentinelCoreAdmin.Core/
├── Contracts/                         # Service interfaces
├── Services/
│   ├── FileService.cs
│   ├── IdentityService.cs
│   └── MicrosoftGraphService.cs
└── Models/
```

## Project: SentinelCore.Tests

**Path**: `projects/SentinelCore.Tests/`
**Type**: Test project (MSTest) | **TFM**: `net10.0-windows10.0.22621.0`
**Project refs**: → Contracts, CaseFlowEngine, Orchestrations
**Note**: Opts out of CPM (`ManagePackageVersionsCentrally=false`)

```
SentinelCore.Tests/
├── GlobalUsings.cs
├── AgentBuilderTests.cs
├── AgentFactoryTests.cs
├── EventPublishingChatClientTests.cs
├── SentinelCoreEventsTests.cs
└── TestInfrastructure/
    ├── CapturingAgentBuilder.cs       # Test double for agent builder
    ├── EventCapture.cs                # Event capture helper
    ├── FakeChatClient.cs              # Fake IChatClient
    ├── NoOpLoggerFactory.cs           # No-op logger factory
    └── TestOptions.cs                 # Test options helper
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
| `ToolingComponent.md` | Tooling design |
| `DomainAgentSurfaces.md` | Domain agent surfaces |
| `DomainToolChart.md` | Domain → tool mapping chart |
| `DynamicAgentsComponent.md` | Dynamic/composite agents |
| `PersistenceComponent.md` | Persistence layer |
| `CaseManagement.md` | Case management |
| `ExampleCaseSchema.md` | Example case schema |
| `Documentation-Manifest.md` | Doc manifest |
| `Proposed-SafetyEngine-Group-Collaboration.md` | Proposed safety engine design |
| `user/ConfigurationAndCustomization.md` | User config guide |

## .github/ Structure

```
.github/
├── copilot-instructions.md            # Copilot instructions (host phasing, conventions)
├── instructions/
│   ├── drift-prevention.instructions.md   # C# drift prevention rules (applyTo: **/*.cs)
│   └── ProjectTerminology.instructions.md # Canonical terminology
├── skills/
│   ├── ArchitectureGate.md            # Architecture gate skill — pre-code checklist
│   └── sentinelcore-navigation/       # THIS skill
├── agents/
│   └── regression-guard.agent.md      # RegressionGuard testing agent
└── prompts/
    └── create-adr.prompt.md           # Create ADR prompt
```
