# SentinelCore — Search Strategies & Navigation Workflows

Detailed strategies for finding code, tracing execution paths, and answering common questions about the SentinelCore repository.

## Strategy 1: "I need to find an agent"

### Question: "Where is the Core agent defined?"

1. The Core agent is not a single class — it's constructed via the **agent factory pipeline**:
   - `Orchestrations\Agents\AgentRole.cs` — defines `AgentRole.Core`
   - `Orchestrations\Agents\AgentProfileBuilder.cs` — builds the `AgentProfile` for `AgentRole.Core`
   - `Orchestrations\Agents\AgentMiddlewarePipeline.cs` — defines the `Core` preset (logging + events + safety + pattern memory + null safety)
   - `Orchestrations\Agents\SentinelAgentFactory.cs` — constructs the actual `AIAgent` from the profile
2. The Core agent's **runtime executor** is `Orchestrations\Workflows\Executors\TheCoreExec.cs`
3. The Core agent's **runner** (wrapping it as an `Executor<SignalHypothesis, string>`) is `Orchestrations\Agents\TheCoreRunner.cs`
4. The Core agent's **MCP tools** are in `Orchestrations\Agents\Core\Tools\`
5. The Core agent's **persona** is looked up in `Orchestrations\Personas\PersonaRegistry.cs`

### Question: "Where is the Manager agent?"

1. `AgentRole.Manager` in `AgentRole.cs`
2. `AgentMiddlewarePipeline.Manager` preset (logging + events only — no tools, no safety)
3. `Orchestrations\Orchestrations\MagneticOrchestration.cs` — the Manager's workflow (`IMagneticOrchestration`)
4. `Orchestrations\Orchestrations\ApprovalBasedManager.cs` — `RoundRobinGroupChatManager` subclass

### Question: "Where are Domain Agents?"

Domain Agents are not predefined classes — they are **dynamically constructed** by the Manager during orchestration:

1. `AgentRole.Utility` is used for domain/composite agents
2. `AgentMiddlewarePipeline.Domain` preset (logging + events + safety)
3. Tools are assigned from `ToolRegistry.cs` based on the domain name
4. Personas are assigned from `PersonaRegistry.cs` based on the task

### Question: "How are agents constructed?"

Trace the full pipeline:

```
AgentRole (enum)
  → AgentProfileBuilder.Build(role) → AgentProfile (record)
    → SentinelAgentFactory.Create(profile)
      → SentinelChatClientFactory.CreateChatClient(model) → IChatClient
        → wrap with middleware (EventPublishingChatClient, ModelNoiseSafety)
        → ChatClientAgent(chatClient, options) → AIAgent
```

**Key rule**: Never construct `ChatClientAgent` directly. Always go through `ISentinelAgentFactory`.

---

## Strategy 2: "I need to find a tool"

### Question: "Where is the registry/firewall/event-log tool?"

1. All Windows diagnostic tools are in `Orchestrations\Tools\` — one file per tool
2. File naming: `*ReadTool.cs` for read-only tools, `*Tool.cs` for others
3. Examples:
   - Registry → `RegistryReadTool.cs`
   - Firewall → `FirewallReadTool.cs`
   - Event Log → `EventLogReadTool.cs`
   - WMI → `WmiQueryTool.cs`
4. Every tool extends `AITool` directly and returns `ToolResult`

### Question: "How are tools mapped to domains?"

1. `Orchestrations\Application\ToolRegistry.cs` — static class mapping 43 domain names to tool class names
2. `docs\DomainToolChart.md` — human-readable domain→tool chart
3. Tools are **NOT** in DI — they are instantiated by domain agents as needed

### Question: "Where are MCP tools?"

1. `Orchestrations\Agents\Core\Tools\MSDocsMCPServerTool.cs` — Microsoft Docs MCP server
2. `Orchestrations\Agents\Core\Tools\SimpleMcpClientTool.cs` — simple MCP client
3. MCP tools are for the **Core agent only**

### Question: "How do I add a new tool?"

1. Create a new file in `Orchestrations\Tools\` (e.g., `MyNewReadTool.cs`)
2. Extend `AITool` directly: `public sealed class MyNewReadTool : AITool`
3. Return `ToolResult.Ok()` or `ToolResult.Fail()` from the tool's execution
4. Use `[Description]` attributes on parameters for AI consumption
5. Add the domain→tool mapping in `ToolRegistry.cs`
6. Update `docs\DomainToolChart.md`

---

## Strategy 3: "I need to trace an execution path"

### Full signal→investigation flow

```
User input (Admin UI: CoreChatViewModel)
  → IOrchestrationControl.InvestigateAsync(signal)
    → OrchestrationControl.cs
      → TheCoreWorkflow.cs (classifies signal, routes via NextStep enum)
        → Executors based on routing decision:
          ├── SafetyExecutor.cs (safety check)
          ├── AgentExecutor.cs (classifier agent)
          ├── TheCoreExec.cs (Core agent — persistent session)
          ├── InvestigationExecutor.cs (starts magnetic investigation)
          │   → MagneticOrchestration.cs (Manager's workflow)
          │     → Domain agents dispatched with tools from ToolRegistry
          ├── NewCaseExecutor.cs (creates case via ICaseFlowEngine)
          ├── PatternCheckExecutor.cs (checks pattern memory)
          ├── DirectAnswerExecutor.cs (direct response)
          ├── MoreInformationExecutor.cs (asks for more info)
          ├── EscalatedExecutor.cs (escalates to human)
          ├── HumanOperatorExecutor.cs (human in the loop)
          └── VerifyEvidenceExecutor.cs (verifies evidence)
```

### Key routing decisions (NextStep enum in CoreRoutingDecision.cs)

| Decision | Executor | Description |
| ---------- | ---------- | ------------- |
| `RedAlert` | `CriticalAlert.cs` | Critical security alert |
| `Investigate` | `InvestigationExecutor.cs` | Start magnetic investigation |
| `MoreInformationRequired` | `MoreInformationExecutor.cs` | Ask for more info |
| `EscalateToHumanOperator` | `HumanOperatorExecutor.cs` | Escalate to human |
| `DirectAnswer` | `DirectAnswerExecutor.cs` | Direct response |

---

## Strategy 4: "I need to find DI registrations"

### The single entry point

```csharp
services.AddSentinelCore(settings);  // in SentinelCoreServiceExtensions.cs
```

### What it registers

| Service | Implementation | Lifetime |
| --------- | --------------- | ---------- |
| `IOrchestrationControl` | `OrchestrationControl` | Singleton |
| `SentinelCoreSettings` | Options pipeline | — |
| `SentinelCoreDbContext` | EF Core (SQL Server) | Transient |
| `IDbContextFactory<SentinelCoreDbContext>` | Pooled factory | Transient |
| `ICaseFlowEngine` | `CaseFlowEngine` | Transient |
| `IEvidenceStore` | `EvidenceStore` | Transient |
| `IPatternMemoryStore` | `PatternMemoryStore` | Transient |
| `ICaseGenerator` | `CaseGenerator` | Transient |
| `ISentinelCoreEvents` | `SentinelCoreEvents` | Singleton |
| `IAgentProfileBuilder` | `AgentProfileBuilder` | Singleton |
| `ISystemReporter` | `SystemReporter` | Singleton |
| `ISentinelWorkflowExecution` | `SentinelWorkflowExecution` | Singleton |
| `TheCoreWorkflow` | — | Singleton |
| `ISentinelAgentFactory` | `SentinelAgentFactory` | Singleton |
| `IOrchestrationFactory` | `OrchestrationFactory` | Singleton |
| `MagneticOrchestration` | — | Singleton |
| All executors | via `RegisterExecutors()` | Transient |

### DI anti-patterns to avoid

- ❌ `services.BuildServiceProvider()` inside registration methods
- ❌ `services.AddLogging()` inside the library — the host owns logging
- ❌ Registering `ISentinelCoreBuilder` as a DI service
- ❌ Registering `ICaseFlowEngine` unconditionally (depends on optional persistence)

---

## Strategy 5: "I need to find safety rules"

1. **Interface**: `Orchestrations\SafetyEngine\ISafetyRule.cs`
2. **Middleware agent**: `Orchestrations\SafetyEngine\SafetyEngineAgent.cs` — intercepts prompts before model
3. **Builder extension**: `Orchestrations\SafetyEngine\SafetyEngineAgentBuilderExtensions.cs` — `UseSafetyEngine()`
4. **15 rule implementations** in `Orchestrations\SafetyEngine\Rules\`:

| Rule | File | What it detects |
| ------ | ------ | ----------------- |
| Blocklist | `BlocklistRule.cs` | Blocked terms/phrases |
| CodeInjection | `CodeInjectionRule.cs` | Code injection attempts |
| Composite | `CompositeRule.cs` | Combines multiple rules |
| DataExfiltration | `DataExfiltrationRule.cs` | Data exfiltration attempts |
| EncodingEvasion | `EncodingEvasionRule.cs` | Encoding-based evasion |
| HarmfulContent | `HarmfulContentRule.cs` | Harmful/dangerous content |
| MaxLength | `MaxLengthRule.cs` | Input length limits |
| PIIDetection | `PIIDetectionRule.cs` | PII in prompts |
| PromptInjection | `PromptInjectionRule.cs` | Prompt injection attacks |
| RegexBlock | `RegexBlockRule.cs` | Regex-based blocking |
| RepetitionAttack | `RepetitionAttackRule.cs` | Repetition-based attacks |
| RoleEscalation | `RoleEscalationRule.cs` | Role escalation attempts |
| SystemPromptExtraction | `SystemPromptExtractionRule.cs` | System prompt extraction |
| TokenLimit | `TokenLimitRule.cs` | Token count limits |
| UrlBlock | `UrlBlockRule.cs` | URL blocking |

---

## Strategy 6: "I need to find middleware"

Two layers of middleware exist:

### Client-level middleware (wrapping IChatClient)

| Middleware | File | Purpose |
| ----------- | ------ | --------- |
| `EventPublishingChatClient` | `Orchestrations\Agents\Middleware\EventPublishingChatClient.cs` | Publishes tool results and agent text to `ISentinelCoreEvents` |
| `ModelNoiseSafety` | `Orchestrations\Agents\ModelNoiseSafety.cs` | Sanitizes model output noise |
| `SafetyEngineAgent` | `Orchestrations\SafetyEngine\SafetyEngineAgent.cs` | Evaluates safety rules before model call |

### Agent-level middleware (via AIAgentBuilder)

| Middleware | File | Purpose |
| ----------- | ------ | --------- |
| `PatternMemoryInjector` | `Orchestrations\Agents\Middleware\PatternMemoryInjector.cs` | RAG pattern memory injection (Core agent only) |
| `SafetyEngineAgent` | `Orchestrations\SafetyEngine\SafetyEngineAgent.cs` | Safety rule evaluation via `UseSafetyEngine()` |

### Extension methods

| Extension | File | Usage |
|-----------|------|-------|
| `UseSentinelEvents()` | `Orchestrations\Agents\SentinelChatClientBuilderExtensions.cs` | Adds event publishing to `ChatClientBuilder` |
| `UseSafetyEngine()` | `Orchestrations\SafetyEngine\SafetyEngineAgentBuilderExtensions.cs` | Adds safety engine to `AIAgentBuilder` |

---

## Strategy 7: "I need to find persistence/EF Core code"

1. **DbContext**: `CaseFlowEngine\Persistence\SentinelCoreDbContext.cs`
2. **Entities**: `CaseFlowEngine\Persistence\*Entity.cs` (Case, Evidence, InvestigationPlan, InvestigationPlanSteps, PatternMemory, Resolution, Signal)
3. **Mappings**: `CaseFlowEngine\Persistence\*MappingExtensions.cs` (entity↔domain model conversion)
4. **Repositories**: `CaseFlowEngine\Infrastructure\Persistence\` (EvidenceStore, PatternMemoryStore, SignalRepository)
5. **DB Initializer**: `CaseFlowEngine\Infrastructure\Persistence\DatabaseInitializer.cs`
6. **Design-time factory**: `CaseFlowEngine\Persistence\DesignTimeSentinelCoreDBContextFactory.cs`
7. **EF Core config**: `CaseFlowEngine\efpt.config.json` (EF Core Power Tools)

---

## Strategy 8: "I need to find tests"

1. **Test project**: `projects/SentinelCore.Tests/`
2. **Test files**: `*Tests.cs` (e.g., `AgentFactoryTests.cs`, `AgentBuilderTests.cs`, `EventPublishingChatClientTests.cs`, `SentinelCoreEventsTests.cs`)
3. **Test infrastructure** (fakes/doubles): `SentinelCore.Tests\TestInfrastructure\`
   - `FakeChatClient.cs` — fake `IChatClient`
   - `CapturingAgentBuilder.cs` — test double for agent builder
   - `EventCapture.cs` — event capture helper
   - `NoOpLoggerFactory.cs` — no-op logger factory
   - `TestOptions.cs` — test options helper
4. **Conventions**: MSTest + Moq, Arrange/Act/Assert comments, `Async` suffix for async test methods
5. **Test naming**: `Method_Condition_ExpectedResult` (per regression-guard agent)

---

## Strategy 9: "I need to find configuration/settings"

1. **Settings model**: `Contracts\Contracts\SentinelCoreSettings.cs` — top-level config
2. **Model profile**: `Contracts\Contracts\ModelProfile.cs` — per-model config (endpoint, modelId, provider, temperature)
3. **Orchestration type**: `Contracts\Contracts\OrchestrationType.cs` — enum (TheCore, CustomGroup)
4. **Host config (legacy)**: `SentinelCoreHost\App.config` — user settings (endpoint, model IDs, temperatures)
5. **Host config (active)**: `SentinelCoreAdmin\appsettings.json` — AppConfig (identity, MSAL, file paths)
6. **Build props**: `Directory.Build.props` (artifacts path), `Directory.Packages.props` (CPM versions)
7. **Env vars**: `UPPER_SNAKE_CASE` naming convention per AGENTS.md

---

## Strategy 10: "I need to find documentation"

| What | Where |
| ------ | ------- |
| Architecture overview | `docs\SolutionArchitecture.md` |
| Component design docs | `docs\*Component.md` |
| Domain→tool chart | `docs\DomainToolChart.md` |
| Terminology | `.github\instructions\ProjectTerminology.instructions.md` or `docs\ProjectTerminology.md` |
| Architectural rules | `.github\instructions\drift-prevention.instructions.md` |
| User config guide | `docs\user\ConfigurationAndCustomization.md` |
| Case management | `docs\CaseManagement.md` |
| Example case schema | `docs\ExampleCaseSchema.md` |
| Doc manifest | `docs\Documentation-Manifest.md` |
| Workflow diagrams | `SentinelCoreHost\workflow.dot`, `SentinelCoreHost\workflow.mermaid` |

---

## Common Search Patterns (PowerShell)

### Find all files matching a naming pattern

```powershell
Get-ChildItem -Path "projects" -Recurse -Filter "*ReadTool.cs" | Select-Object FullName
```

### Find all AITool subclasses

```powershell
Select-String -Path "projects\*\*\*.cs" -Pattern ":\s*AITool\b" | Select-Object Path, LineNumber
```

### Find all executors

```powershell
Get-ChildItem -Path "projects\SentinelCore.Orchestrations\Workflows\Executors" -Filter "*Executor.cs" | Select-Object Name
```

### Find all DI registration methods

```powershell
Select-String -Path "projects\*\*\*.cs" -Pattern "AddSentinelCore|AddCaseFlowEngine|RegisterExecutors" | Select-Object Path, LineNumber
```

### Find all safety rules

```powershell
Get-ChildItem -Path "projects\SentinelCore.Orchestrations\SafetyEngine\Rules" -Filter "*Rule.cs" | Select-Object Name
```

### Find where a specific interface is used

```powershell
Select-String -Path "projects\*\*\*.cs" -Pattern "ISentinelCoreEvents" | Select-Object Path, LineNumber, Line
```
