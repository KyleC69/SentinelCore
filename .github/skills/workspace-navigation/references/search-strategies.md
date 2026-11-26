# SentinelCore — Search Strategies & Navigation Workflows

Detailed strategies for finding code, tracing execution paths, and answering common questions about the SentinelCore repository.

> All paths are relative to the repository root `SentinelCore/` unless noted. The out-of-tree projects (`SentinelCoreSecurity`, `SentinelCore.Tests`) live as siblings of the repo root — see the SKILL.md workspace layout.

## Strategy 1: "I need to find an agent"

### Question: "Where is the Core agent defined?"

1. The Core agent is not a single class — it's constructed via the **agent factory pipeline**:
   - `Orchestrations\Agents\AgentPresets\AgentPresetDefinition.cs` — immutable preset record (id, name, model profile, tier, tool groups, middleware flags)
   - `Orchestrations\Agents\AgentInstructionConstants.cs` — static role instruction texts
   - `Orchestrations\Agents\ModelResolver.cs` — resolves the `ModelProfile` per tier/role
   - `Orchestrations\Agents\SentinelAgentFactory.cs` — constructs the actual `AIAgent` from the preset
2. The Core agent's **runtime executor** is `Orchestrations\Workflows\Executors\TheCoreExec.cs` (persistent session)
3. The Core agent's **switch/routing executor** is `Orchestrations\Workflows\Executors\TheCoreSwitchExec.cs`
4. The Core agent's **own tools** are in `Orchestrations\Agents\Core\Tools\` (`AgentSpawnTools`, `CaseEngineTools`, `MsLearnMcpTools`)
5. The Core agent's **persona** is looked up in `Orchestrations\Personas\PersonaRegistry.cs`
6. The Core agent's **pattern memory injection** is `Orchestrations\Agents\Middleware\PatternMemoryInjector.cs` (Core agent only)

### Question: "Where is the Manager agent?"

1. Manager preset comes from `AgentPresetDefinition` (via `ISentinelAgentFactory`) — **the Manager must have zero tools** (PL-3)
2. `Orchestrations\Orchestrations\ApprovalBasedManager.cs` — `RoundRobinGroupChatManager` subclass (approval gate)
3. Manager prompts: `Orchestrations\Orchestrations\MagenticDefaultPrompts.cs` and `MagenticPromptOverrides.cs`
4. The Manager's **team workflow** is built with MAF `MagenticWorkflowBuilder` inside `Orchestrations\Workflows\TheCoreWorkflow.cs` (workers: evidence collector, adversarial analyst, researcher, final report writer)

### Question: "Where are Domain/Worker Agents?"

Domain/worker agents are not predefined classes — they are **dynamically constructed** per investigation:

1. Presets declare capabilities via `AgentPresetDefinition` in `Orchestrations\Agents\AgentPresets\`
2. Tools are assigned through tool groups from `Orchestrations\Abstractions\AgentToolGroupCatalog.cs`
3. Personas are assigned from `Orchestrations\Personas\PersonaRegistry.cs` based on the task

### Question: "How are agents constructed?"

Trace the full pipeline:

```
AgentPresetDefinition (immutable preset)
  → SentinelAgentFactory (ISentinelAgentFactory)
    → SentinelChatClientFactory.CreateChatClient(model) → IChatClient
      → wrap with middleware (safety engine, RAG, event publishing per preset flags)
      → ChatClientAgent(chatClient, options) → AIAgent
```

**Key rule**: Never construct `ChatClientAgent` directly. Always go through `ISentinelAgentFactory` with an `AgentPresetDefinition` (PL-3). Presets may be overridden at the call site for instructions, tools, and response format.

---

## Strategy 2: "I need to find a tool"

Tools live in **two places**, depending on who consumes them:

### Windows diagnostic/scouting tools (delivered via MCP)

1. All Windows diagnostic tools are in `projects\SentinelCore.MCPTools\Tools\` — one file per domain
2. File naming: `*ReadTool.cs` for read-only tools, `*ExtendedReadTool.cs` for extended variants, `*Tool.cs` for others
3. Registration: `Tools\MCPToolRegistration.cs` + `McpToolFactory.cs`; the authoritative live catalogue is surfaced by `sc_utilities_read_server_manifest`
4. Naming: `sc_<groupprefix>_<verb>_<noun>` (ADR-006); canonical spec: `SentinelCore.MCPTools\Docs\ToolingComponent.md`
5. Examples: Registry → `RegistryReadTool.cs` / `RegistryExtendedReadTool.cs`, Firewall → `FirewallReadTool.cs`, Event Log → `EventLogReadTool.cs` / `EventLogExtendedReadTool.cs`, WMI → `WmiQueryTool.cs`
6. Every tool returns `ToolResult` (`Tools\ToolResult.cs`) and never throws

### Tools used by the Core agent (in-process)

1. `Orchestrations\Agents\Core\Tools\AgentSpawnTools.cs` — spawning worker agents
2. `Orchestrations\Agents\Core\Tools\CaseEngineTools.cs` — case engine operations
3. `Orchestrations\Agents\Core\Tools\MsLearnMcpTools.cs` — Microsoft Learn MCP (temporary, ADR-007)
4. `Orchestrations\Agents\Core\Tools\ToolResult.cs` — Core tool result type
5. `Orchestrations\Agents\Core\Tools\README.md` — documentation

### Question: "How are tools mapped to groups?"

1. `Orchestrations\Abstractions\AgentToolGroupCatalog.cs` — the group registry; one `sc_<groupprefix>_` prefix per group
2. `Orchestrations\Agents\AgentPresets\AgentPresetDefinition.cs` — the groups each preset declares (`ToolGroups`)
3. `Orchestrations\Agents\SentinelAgentFactory.cs` — resolves a preset's groups, or the call-site override, to tools
4. `docs\ToolingComponent.md` — authoritative description of the scheme

### Question: "How do I add a new tool?"

1. Name it `sc_<groupprefix>_<verb>_<noun>`, using the prefix of the group that should own it
2. Add it to `SentinelCore.MCPTools\Tools\` and register it in `MCPToolRegistration.cs`; return `ToolResult`, mark `ReadOnly = true`
3. Add that group to the presets that should receive it in `AgentPresetDefinition.ToolGroups`
4. If no group fits, register one in `AgentToolGroupCatalog` — one prefix, one name
5. No mapping table is edited; discovery is by name prefix (see `docs\ToolingComponent.md` and ADR-006)

### Question: "Where are MCP client connections managed?"

`Orchestrations\Mcp\` — `McpConnectionFactory.cs`, `McpServerRegistry.cs`, `JsonFileMcpServerRegistryStore.cs`, `McpToolTimeoutFunction.cs` (timeout wrapper), `DpapiTokenCache.cs` (token cache), `LoopbackOAuthCallbackHandler.cs` (OAuth flow). Registry DTOs/contracts live in `SentinelCore.Contracts\Mcp\`.

## Strategy 3: "I need to trace an execution path"

### Full signal→investigation flow

```
User input (WPF UI: CoreChatViewModel)
  → IOrchestrationControl (OrchestrationControl.cs)
    → TheCoreWorkflow.cs (classifies signal, routes via NextStep enum)
      → Executors based on routing decision:
        ├── SignalClassifierExecutor.cs (signal classification)
        ├── SafetyExecutor.cs / SafetyReviewExecutor.cs (safety checks)
        ├── TheCoreSwitchExec.cs (provenance-aware core routing)
        ├── TheCoreExec.cs (Core agent — persistent session)
        ├── ProvenanceGateExecutor.cs (fail-closed provenance check)
        ├── GenerateInvestigationObjective.cs (objective generation)
        ├── NewCaseExecutor.cs (creates case via ICaseFlowEngine)
        ├── PatternCheckExecutor.cs (checks pattern memory)
        ├── SubWorkflowExecutor.cs (runs magnetic/magentic investigation sub-workflow)
        ├── DirectAnswerExecutor.cs (direct response)
        ├── MoreInformationExecutor.cs (asks for more info)
        ├── EscalatedExecutor.cs (escalates to human)
        ├── HumanOperatorExecutor.cs (human in the loop)
        ├── CaseUpdateExecutor.cs (case updates)
        ├── PersistEvidence.cs (evidence persistence)
        ├── EvaluationExec.cs / HarnessTest.cs (evaluation/harness)
        ├── CriticalAlert.cs (critical alerts)
        ├── WhiteListExecutor.cs (whitelist routing)
        └── TerminateWorkflow.cs (termination)
```

### Key routing decisions (NextStep enum in Workflows\NextStep.cs)

| Decision | Description |
| ---------- | ------------- |
| `RedAlert` | Critical — immediate escalation to human operator |
| `Investigate` | Run magnetic investigation sub-workflow (worker agents) |
| `MoreInformationRequired` | Ask for more info |
| `EscalateToHumanOperator` | Escalate to human (edge case from any sub-workflow step) |
| `DirectAnswer` | Direct response |

Core-switch routing uses `Models\CoreSwitchDecision.cs`.

---

## Strategy 4: "I need to find DI registrations"

### The single entry point

```csharp
services.AddSentinelCore(settings);  // in SentinelCoreServiceExtensions.cs (Orchestrations\Infrastructure\DependencyInjection\)
```

CaseFlowEngine modules register via `CaseFlowEngineBuilderExtensions.cs` (`CaseFlowEngine\Infrastructure\DependencyInjection\`); the security module via `SentinelSecurityServiceExtensions` in the out-of-tree `SentinelCoreSecurity\Infrastructure\DependencyInjection\`.

### DI anti-patterns to avoid

- ❌ `services.BuildServiceProvider()` inside registration methods
- ❌ `services.AddLogging()` inside the library — the host owns logging
- ❌ Registering `ICaseFlowEngine` unconditionally (depends on optional persistence)
- ❌ Null-object stores/throwing Null* implementations — Null persistence returns no-op results (PL)

For the authoritative, current registration list read `SentinelCoreServiceExtensions.cs` directly — the service set changes frequently; this map intentionally does not duplicate it.

---

## Strategy 5: "I need to find safety rules"

1. **Interface**: `Orchestrations\SafetyEngine\ISafetyRule.cs`
2. **Rule engine**: `Orchestrations\SafetyEngine\SafetyRuleEngine.cs` — aggregates rules
3. **Middleware agent**: `Orchestrations\SafetyEngine\SafetyEngineAgent.cs` — intercepts prompts before model
4. **Builder extension**: `Orchestrations\SafetyEngine\SafetyEngineAgentBuilderExtensions.cs` — `UseSafetyEngine()`
5. **Options/taxonomy**: `SafetyEngineOptions.cs`, `SafetySeverity.cs`, `SafetyRuleResult.cs`, `SafetyEvaluationContext.cs`, `SafetyEvaluationResult.cs`, `SafetyTagger.cs` (tagging), `BlockPhrases.cs` (static phrase list)
6. **18 rule implementations** in `Orchestrations\SafetyEngine\Rules\`:

| Rule | File | What it detects |
| ------ | ------ | ----------------- |
| Blocklist | `BlocklistRule.cs` | Blocked terms/phrases |
| CodeInjection | `CodeInjectionRule.cs` | Code injection attempts |
| Composite | `CompositeRule.cs` | Combines multiple rules |
| ContentFilter | `ContentFilterRule.cs` | Content filtering |
| DataExfiltration | `DataExfiltrationRule.cs` | Data exfiltration attempts |
| EncodingEvasion | `EncodingEvasionRule.cs` | Encoding-based evasion |
| HarmfulContent | `HarmfulContentRule.cs` | Harmful/dangerous content |
| MaxLength | `MaxLengthRule.cs` | Input length limits |
| OutputSanitizer | `OutputSanitizerRule.cs` | Sanitizes model output |
| PIIDetection | `PIIDetectionRule.cs` | PII in prompts |
| PromptInjection | `PromptInjectionRule.cs` | Prompt injection attacks |
| RateLimit | `RateLimitRule.cs` | Call-rate limits |
| RegexBlock | `RegexBlockRule.cs` | Regex-based blocking |
| RepetitionAttack | `RepetitionAttackRule.cs` | Repetition-based attacks |
| RoleEscalation | `RoleEscalationRule.cs` | Role escalation attempts |
| SystemPromptExtraction | `SystemPromptExtractionRule.cs` | System prompt extraction |
| TokenLimit | `TokenLimitRule.cs` | Token count limits |
| UrlBlock | `UrlBlockRule.cs` | URL blocking |

> `SafetyEngineTranscript.jsonl` in the same folder is a large design-transcript artifact — exclude it from code searches.

---

## Strategy 6: "I need to find middleware"

Two layers of middleware exist:

### Agent-level middleware (via builder pipeline, driven by presets)

| Middleware | File | Purpose |
| ----------- | ------ | --------- |
| `SafetyEngineAgent` | `Orchestrations\SafetyEngine\SafetyEngineAgent.cs` | Safety rule evaluation via `UseSafetyEngine()` |
| `PatternMemoryInjector` | `Orchestrations\Agents\Middleware\PatternMemoryInjector.cs` | RAG pattern memory injection (Core agent only) |
| `RagContextInjector` / `RagMiddlewarePipeline` | `Orchestrations\Rag\` | RAG context injection pipeline |
| `AdvancedInMemoryChatHistoryProvider` | `Orchestrations\Providers\` | Chat history provider |
| Provenance stamping/validation | `SentinelCoreSecurity` + `Orchestrations\Models\ChatMessageExtensions.cs` | ChatMessage provenance metadata (ADR-008) |

### Client-level factory

| Component | File | Purpose |
| ----------- | ------ | --------- |
| `SentinelChatClientFactory` | `Orchestrations\Agents\SentinelChatClientFactory.cs` | Creates `IChatClient` per provider (Ollama) |
| `ModelResolver` | `Orchestrations\Agents\ModelResolver.cs` | Resolves `ModelProfile` per tier/role |

### Extension methods

| Extension | File | Usage |
|-----------|------|-------|
| `UseSafetyEngine()` | `Orchestrations\SafetyEngine\SafetyEngineAgentBuilderExtensions.cs` | Adds safety engine to agent builder |
| CaseFlowEngine module extensions | `CaseFlowEngine\Infrastructure\DependencyInjection\CaseFlowEngineBuilderExtensions.cs` | Registers CFE services |

> `EventPublishingChatClient` and `ModelNoiseSafety` no longer exist; event output flows through `ISystemReporter` / `ISentinelCoreEvents` and `Orchestrations\Services\WorkflowEventProcessor.cs`.

---

## Strategy 7: "I need to find persistence/EF Core code"

1. **Case-flow DbContext**: `CaseFlowEngine\DBEntities\SentinelCoreDBContext.cs` (+ partials `.Functions.cs`, `SentinelCoreDBContextProcedures.cs`; DB `SENTINEL_CORE`)
2. **Entities**: `CaseFlowEngine\DBEntities\*Entity.cs` (Artifact, Case, Evidence, PatternMemory, Remediation, Signal; plus `InvestigationObjective.cs`)
3. **RAG/pattern-memory context**: `CaseFlowEngine\Persistence\SentinelRAGDBContext*.cs` (+ procedures)
4. **Mappings**: `CaseFlowEngine\Persistence\*MappingExtensions.cs` and `DBEntities\DbContextExtensions.cs` (entity↔domain conversion, SqlQueryAsync helper)
5. **Repositories/stores**: `CaseFlowEngine\Persistence\` (`EvidenceStore`, `PatternMemoryStore`, `SignalRepository`) — interfaces in `Contracts\Abstractions\`
6. **No-op persistence**: `CaseFlowEngine\Persistence\NullPersistenceServices.cs` — Null* stores return no-op results, never throw
7. **Design-time factory**: `CaseFlowEngine\Persistence\DesignTimeSentinelCoreDBContextFactory.cs`
8. **Migrations**: `CaseFlowEngine\Migrations\` + `migration.sql`
9. **EF Core config**: `CaseFlowEngine\efpt.config.json` (EF Core Power Tools), `dab-options.json` (Data API builder)

---

## Strategy 8: "I need to find tests"

1. **Test project location**: `F:\Solutions\SentinelCore.Tests\` — sibling of the workspace root (**out-of-tree**, not referenced by `SentinelCore.slnx`; verify csproj/TFM on disk before building)
2. **Conventions** (per root instructions): MSTest + Moq (mock only process boundaries/3rd-party APIs), Arrange/Act/Assert comments, `Async` suffix, deterministic tests, coverage for abnormal state changes and adversarial cases, `Method_Condition_ExpectedResult` naming (per regression-Test agent)
3. **ADR-008 grants** `InternalsVisibleTo("SentinelCore.Tests")` for deterministic provenance stamp construction

---

## Strategy 9: "I need to find configuration/settings"

1. **Settings model**: `Contracts\Contracts\SentinelCoreSettings.cs` — top-level config
2. **Model profile**: `Contracts\Contracts\ModelProfile.cs` — per-model config (endpoint, modelId, provider, temperature)
3. **Model tiers**: `Contracts\Contracts\ModelTier.cs` — per-role default model tiers
4. **MCP registry DTOs**: `Contracts\Mcp\` (definition, info, status, transport, OAuth settings)
5. **Host config (Service)**: `projects\SentinelCoreService\App.config`
6. **MCP server manifest**: `projects\SentinelCore.MCPTools\.mcp\server.json`
7. **CaseFlowEngine local env**: `projects\SentinelCore.CaseFlowEngine\.env`
8. **Security options**: `F:\Solutions\SentinelCoreSecurity\SentinelSecurityOptions.cs` (key from `SENTINEL_KEY` env var)
9. **Version pinning**: `Directory.Build.props`, `Directory.Packages.props` (CPM), `global.json` (SDK pin)
10. **Env vars**: `UPPER_SNAKE_CASE` naming — read from environment variables per root instructions

---

## Strategy 10: "I need to find documentation"

| What | Where |
| ------ | ------- |
| Architecture overview | `docs\SolutionArchitecture.md` |
| Component design docs | `docs\*Component.md` |
| Tool groups and selection | `docs\ToolingComponent.md` + `SentinelCore.MCPTools\Docs\ToolingComponent.md` |
| MCP server spec | `SentinelCore.MCPTools\Docs\MCPServerToolStandardSpecification.md` |
| Terminology | `.github\instructions\ProjectTerminology.instructions.md` or `docs\ProjectTerminology.md` |
| Architectural rules | `.github\instructions\drift-prevention.instructions.md` + `architecture\pattern-lock.md` (authoritative) |
| ADRs | `docs\ADR\` (001–008) |
| Case management | `docs\CaseManagement.md` |
| Example case schema | `docs\ExampleCaseSchema.md` |
| Doc manifest | `docs\Documentation-Manifest.md` |
| Workflow diagram | `projects\SentinelCore.Orchestrations\Workflows\workflow.dot` |

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

### Find all MCP tools (SentinelCore-MCP server)

```powershell
Get-ChildItem -Path "projects\SentinelCore.MCPTools\Tools" -Filter "*Tool.cs" | Select-Object Name
```

### Find all executors

```powershell
Get-ChildItem -Path "projects\SentinelCore.Orchestrations\Workflows\Executors" -Filter "*.cs" | Select-Object Name
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

> **Tip:** Never grep inside `SafetyEngineTranscript.jsonl` — it is a multi-megabyte design transcript. Exclude `*.jsonl` from broad searches to keep them fast.
