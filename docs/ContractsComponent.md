---
title: "Contracts & Abstractions Component"
status: Active
component: Contracts
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Contracts & Abstractions Component

**Project:** `SentinelCore.Contracts`
**Namespaces:** `SentinelCore.Contracts.Abstractions`, `SentinelCore.Contracts.CaseFlow`, `SentinelCore.Contracts.Contracts`, `SentinelCore.Contracts.Events`, `SentinelCore.Contracts.SafetyEngine`
**Dependencies:** `Microsoft.Extensions.AI`, `System.ComponentModel.DataAnnotations`
**Consumers:** `SentinelCore.Orchestrations`, `SentinelCore.CaseFlowEngine`, `SentinelCoreHost`

---

## Purpose

The Contracts component is the **foundation layer** of SentinelCore — it defines all shared abstractions, DTOs, enums, events, and configuration types with **zero dependencies** on other SentinelCore projects. It is the contract surface that both Orchestrations and CaseFlowEngine implement against.

**Architectural Rule:** `SentinelCore.Contracts` has **no project references** to any other SentinelCore project. It is the stable, versioned API surface.

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Abstractions (Repositories, Stores)                      │  │
│  │  - ICaseRepository                                        │  │
│  │  - ISignalRepository                                      │  │
│  │  - IEvidenceStore                                         │  │
│  │  - IPatternMemoryStore                                    │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  CaseFlow (DTOs, Enums, Aggregates)                       │  │
│  │  - Case, Signal, Evidence, InvestigationPlan,             │  │
│  │    InvestigationPlanStep, Resolution, PatternMemory       │  │
│  │  - CaseStatus (13 states)                                 │  │
│  │  - InvestigationSurface (12 domains)                      │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Contracts (Settings, Agent Types)                        │  │
│  │  - SentinelCoreSettings                                   │  │
│  │  - ModelSettings (per-role)                               │  │
│  │  - AgentRole (6 roles)                                    │  │
│  │  - AgentSpec, AgentPersona                                │  │
│  │  - OrchestrationType (7 types)                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Events (Host Communication)                              │  │
│  │  - ISentinelCoreEvents (7 channels)                       │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  SafetyEngine (Safety Abstractions)                       │  │
│  │  - ISafetyMiddleware, ISafetyRule                         │  │
│  │  - SafetyContext, SafetyResult, SafetyVerdict             │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
         ▲                    ▲                    ▲
         │ implements       │ implements         │ implements
         ▼                  ▼                    ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ Orchestrations   │ │ CaseFlowEngine   │ │ SentinelCoreHost │
└──────────────────┘ └──────────────────┘ └──────────────────┘
```

---

## 1. Abstractions (Repositories & Stores)

### ICaseRepository

**File:** `Abstractions/ICaseRepository.cs`

```csharp
public interface ICaseRepository
{
    /// <summary>Creates a new case record.</summary>
    Task<Case> CreateAsync(Case caseRecord, CancellationToken ct = default);

    /// <summary>Creates a case with its initiating signal in one transaction.</summary>
    Task<Case> CreateCaseWithSignalAsync(Case caseRecord, Signal signal, CancellationToken ct = default);

    /// <summary>Retrieves a case by business CaseId with all navigation properties.</summary>
    Task<Case?> GetByIdAsync(string caseId, CancellationToken ct = default);

    /// <summary>Lists cases with optional status filter and pagination.</summary>
    Task<IReadOnlyList<Case>> ListAsync(CaseStatus? status = null, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Updates an existing case (full aggregate).</summary>
    Task<Case> UpdateAsync(Case caseRecord, CancellationToken ct = default);
}
```

### ISignalRepository

**File:** `Abstractions/ISignalRepository.cs`

```csharp
public interface ISignalRepository
{
    /// <summary>Adds a new signal.</summary>
    Task<Signal> AddAsync(Signal signal, CancellationToken ct = default);

    /// <summary>Assigns an existing signal to a case.</summary>
    Task<Signal> AssignToCaseAsync(string signalId, string caseId, CancellationToken ct = default);
}
```

### IEvidenceStore

**File:** `Abstractions/IEvidenceStore.cs`

```csharp
public interface IEvidenceStore
{
    /// <summary>Adds evidence to a case.</summary>
    Task<Evidence> AddAsync(Evidence evidence, CancellationToken ct = default);

    /// <summary>Retrieves all evidence for a case, ordered by timestamp.</summary>
    Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(string caseId, CancellationToken ct = default);
}
```

### IPatternMemoryStore

**File:** `Abstractions/IPatternMemoryStore.cs`

```csharp
public interface IPatternMemoryStore
{
    /// <summary>Searches for similar pattern memories using vector similarity.</summary>
    Task<IReadOnlyList<PatternMemory>> SearchAsync(
        ReadOnlyMemory<float> embedding,
        int topK = 5,
        float threshold = 0.7f,
        CancellationToken ct = default);

    /// <summary>Stores a new pattern memory with embeddings.</summary>
    Task StoreAsync(PatternMemory memory, CancellationToken ct = default);

    /// <summary>Retrieves pattern memory by case ID.</summary>
    Task<PatternMemory?> GetByCaseIdAsync(string caseId, CancellationToken ct = default);
}
```

---

## 2. CaseFlow DTOs & Enums

### Case (Aggregate Root)

**File:** `CaseFlow/Case.cs`

```csharp
public sealed record Case
{
    /// <summary>Internal record ID (Guid as string).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Business case ID (human-readable).</summary>
    public string CaseId { get; init; } = string.Empty;

    /// <summary>Collection of evidence items.</summary>
    public IReadOnlyList<Evidence> EvidenceItems { get; init; } = [];

    /// <summary>The signal that initiated this case.</summary>
    public Signal? InitiatingSignal { get; init; }

    /// <summary>Pattern memory ID for this case.</summary>
    public string? PatternMemoryId { get; init; }

    /// <summary>Active investigation plan ID.</summary>
    public string? PlanId { get; init; }

    /// <summary>All signals associated with this case.</summary>
    public IReadOnlyList<Signal> Signals { get; init; } = [];

    /// <summary>Current case status.</summary>
    public CaseStatus Status { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Investigation plan (if any).</summary>
    public InvestigationPlan? Plan { get; init; }

    /// <summary>Case resolution (if resolved).</summary>
    public Resolution? Resolution { get; init; }
}
```

### CaseStatus (13 States)

**File:** `CaseFlow/CaseStatus.cs`

```csharp
public enum CaseStatus
{
    /// <summary>Case created, awaiting initial processing.</summary>
    Created = 0,

    /// <summary>Signal received, case initialized.</summary>
    SignalReceived = 1,

    /// <summary>Core agent analyzing signal, creating investigation plan.</summary>
    Planning = 2,

    /// <summary>Plan created, ready for execution.</summary>
    PlanReady = 3,

    /// <summary>Magnetic orchestration executing investigation steps.</summary>
    Investigating = 4,

    /// <summary>Evidence being collected by domain agents.</summary>
    EvidenceGathering = 5,

    /// <summary>Core agent analyzing collected evidence.</summary>
    Analyzing = 6,

    /// <summary>Root cause hypothesized, resolution being formulated.</summary>
    Resolving = 7,

    /// <summary>Resolution proposed, awaiting verification.</summary>
    ResolutionProposed = 8,

    /// <summary>Resolution verified, case closed successfully.</summary>
    Resolved = 9,

    /// <summary>Case closed without resolution.</summary>
    Closed = 10,

    /// <summary>Case blocked, requires human intervention.</summary>
    Blocked = 11,

    /// <summary>Case failed due to error.</summary>
    Failed = 12
}
```

**State Transitions (enforced by CaseFlowEngine):**
```
Created → SignalReceived → Planning → PlanReady → Investigating
                                                      ↓
                                              EvidenceGathering
                                                      ↓
                                              Analyzing → Resolving → ResolutionProposed
                                                      ↓                    ↓
                                              (blocked)           Resolved / Closed
                                                      ↓
                                              Failed
```

### Signal

**File:** `CaseFlow/Signal.cs`

```csharp
public sealed record Signal
{
    public string Id { get; init; } = string.Empty;
    public string SignalId { get; init; } = string.Empty;
    public string CaseRecordId { get; init; } = string.Empty;
    public string SignalText { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}
```

### Evidence

**File:** `CaseFlow/Evidence.cs`

```csharp
public sealed record Evidence
{
    public Case? Case { get; init; }
    public string CaseRecordId { get; init; } = string.Empty;
    public string ContentJson { get; init; } = string.Empty;
    public string EvidenceId { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Provenance { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = string.Empty;
}
```

### InvestigationPlan & Step

**File:** `CaseFlow/InvestigationPlan.cs`, `CaseFlow/InvestigationPlanStep.cs`

```csharp
public sealed record InvestigationPlan
{
    public string Id { get; init; } = string.Empty;
    public string PlanId { get; init; } = string.Empty;
    public IReadOnlyList<InvestigationPlanStep> Steps { get; init; } = [];
    public string CaseId { get; init; } = string.Empty;
}

public sealed record InvestigationPlanStep
{
    public bool CompletedSuccessfully { get; init; }
    public string Id { get; init; } = string.Empty;
    public string Instruction { get; init; } = string.Empty;
    public bool IsTargetPropertyMissing { get; init; }
    public string? OperationId { get; init; }
    public InvestigationPlan? Plan { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string? Result { get; init; }
    public string StepId { get; init; } = string.Empty;
    public InvestigationSurface Surface { get; init; }
    public bool TaskBlocked { get; init; }
}
```

### InvestigationSurface (12 Domains)

**File:** `CaseFlow/InvestigationSurface.cs`

```csharp
public enum InvestigationSurface
{
    Registry = 0,
    FileSystem = 1,
    Environment = 2,
    BootConfig = 3,
    Accessibility = 4,
    SearchIndexing = 5,
    ShellExplorer = 6,
    Certificates = 7,
    EventLog = 8,
    AppLocker = 9,
    WindowsUpdate = 10,
    PnPDevices = 11,
    HyperV = 12,
    Audio = 13,
    Printers = 14,
    GroupPolicy = 15,
    Firewall = 16,
    LocalAccounts = 17,
    RDP = 18,
    Services = 19,
    ScheduledTasks = 20,
    Power = 21,
    Network = 22,
    DCOM = 23,
    WMI = 24,
    Drivers = 25,
    Processes = 26,
    Performance = 27,
    InstalledApps = 28,
    BrowserConfig = 29,
    Fonts = 30,
    Notifications = 31,
    VPN = 32,
    Wireless = 33,
    Proxy = 34,
    Sensors = 35,
    Battery = 36,
    Display = 37,
    Credentials = 38,
    UAC = 39,
    Defender = 40,
    BitLocker = 41
}
```

### Resolution

**File:** `CaseFlow/Resolution.cs`

```csharp
public sealed record Resolution
{
    public string CaseRecordId { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string RawJsonContent { get; init; } = string.Empty;
    public bool Verified { get; init; }
}
```

### PatternMemory

**File:** `CaseFlow/PatternMemory.cs`

```csharp
public sealed record PatternMemory
{
    public Case? Case { get; init; }
    public string CaseId { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string PatternId { get; init; } = string.Empty;
    public ReadOnlyMemory<float> SignalEmbedding { get; init; } = ReadOnlyMemory<float>.Empty;
    public ReadOnlyMemory<float> SummaryEmbedding { get; init; } = ReadOnlyMemory<float>.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}
```

---

## 3. Contracts (Settings & Agent Types)

### SentinelCoreSettings

**File:** `Contracts/SentinelCoreSettings.cs`

```csharp
public sealed class SentinelCoreSettings
{
    /// <summary>Model configuration for Core agent.</summary>
    public ModelSettings? CoreModel { get; set; }

    /// <summary>Model configuration for Manager agent.</summary>
    public ModelSettings? ManagerModel { get; set; }

    /// <summary>Model configuration for Domain agents.</summary>
    public ModelSettings? DomainModel { get; set; }

    /// <summary>Default model for Worker, General, Aggregator agents.</summary>
    public ModelSettings? DefaultModel { get; set; }

    /// <summary>SQL Server connection string.</summary>
    public string SqlConnectionString { get; set; } = string.Empty;

    /// <summary>Enable distributed tracing.</summary>
    public bool TraceEnabled { get; set; } = true;

    /// <summary>Orchestration strategy to use.</summary>
    public OrchestrationType OrchestrationType { get; set; } = OrchestrationType.MagneticCoop;
}
```

### ModelSettings

**File:** `Contracts/ModelSettings.cs`

```csharp
public sealed class ModelSettings
{
    public ModelSettings(string endpoint, string modelId,
                         float temperature = 0.1f,
                         int? maxOutputTokens = 4000,
                         int topK = 1,
                         float topP = 1.0f)
    {
        Endpoint = endpoint;
        ModelId = modelId;
        Temperature = temperature;
        MaxOutputTokens = maxOutputTokens;
        TopK = topK;
        TopP = topP;
    }

    public string Endpoint { get; init; } = "http://127.0.0.1:11434";
    public string ModelId { get; init; } = string.Empty;
    public float Temperature { get; set; } = 0.1f;
    public int TopK { get; set; } = 1;
    public float TopP { get; set; } = 1.0f;
    public int? MaxOutputTokens { get; set; } = 4000;
}
```

### AgentRole (6 Roles)

**File:** `Contracts/AgentRole.cs` (also in Orchestrations.Agents)

```csharp
public enum AgentRole
{
    /// <summary>The Core reasoning agent — application lifetime, research & case tools.</summary>
    Core,

    /// <summary>The Magnetic Orchestration Manager — workflow lifetime, no tools.</summary>
    Manager,

    /// <summary>A predefined Domain agent — per-task lifetime, domain-specific toolbelt.</summary>
    Domain,

    /// <summary>A multi-domain agent for cooperative workflows — per-task lifetime, general toolbelt.</summary>
    Worker,

    /// <summary>A general purpose agent — per-task lifetime.</summary>
    General,

    /// <summary>An aggregator agent — per-task lifetime, responsible for aggregating results.</summary>
    Aggregator
}
```

### AgentSpec

**File:** `Contracts/AgentSpec.cs` (also in Orchestrations.Agents)

```csharp
public sealed record AgentSpec
{
    public AgentRole Role { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public AgentPersona Persona { get; init; } = new();
    public IReadOnlyList<AITool> Tools { get; init; } = [];
    public ModelSettings Model { get; init; } = new("http://127.0.0.1:11434", string.Empty);
}
```

### AgentPersona

**File:** `Contracts/AgentPersona.cs` (also in Orchestrations.Personas)

```csharp
public record AgentPersona : IAgentPersona
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public interface IAgentPersona
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
}
```

### OrchestrationType (7 Types)

**File:** `Contracts/OrchestrationType.cs`

```csharp
public enum OrchestrationType
{
    /// <summary>Single Core agent handles everything.</summary>
    SingleAgent = 0,

    /// <summary>Sequential agent pipeline.</summary>
    Sequential = 1,

    /// <summary>Turn-based group conversation.</summary>
    GroupTurnBased = 2,

    /// <summary>Concurrent group workflow.</summary>
    GroupConcurrent = 3,

    /// <summary>Magnetic cooperative (Manager + Workers + Critic + Aggregator).</summary>
    MagneticCoop = 4,

    /// <summary>Core agent + Magnetic sub-workflow (primary production mode).</summary>
    TheCore = 5,

    /// <summary>Legacy magnetic orchestration.</summary>
    Magnetic = 6
}
```

---

## 4. Events (Host Communication)

### ISentinelCoreEvents

**File:** `Events/ISentinelCoreEvents.cs`

```csharp
public interface ISentinelCoreEvents
{
    /// <summary>Core agent activity events (tool calls, reasoning).</summary>
    Task RaiseTheCoreActivityAsync(string activity, CancellationToken ct = default);

    /// <summary>Core agent reasoning traces.</summary>
    Task RaiseTheCoreReasoningAsync(string reasoning, CancellationToken ct = default);

    /// <summary>Core agent tool invocations.</summary>
    Task RaiseTheCoreToolingAsync(string toolName, string input, string output, CancellationToken ct = default);

    /// <summary>Magnetic orchestration participant activity.</summary>
    Task RaiseMagneticParticipantActivityAsync(string agentName, string activity, CancellationToken ct = default);

    /// <summary>Case lifecycle events.</summary>
    Task RaiseCaseLifecycleAsync(string caseId, CaseStatus status, CancellationToken ct = default);

    /// <summary>Evidence collected events.</summary>
    Task RaiseEvidenceCollectedAsync(string caseId, Evidence evidence, CancellationToken ct = default);

    /// <summary>Safety violation events.</summary>
    Task RaiseSafetyViolationAsync(string ruleId, string reason, AgentRole agentRole, CancellationToken ct = default);

    /// <summary>Safety flag events (allowed but monitored).</summary>
    Task RaiseSafetyFlagAsync(string ruleId, string reason, AgentRole agentRole, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct = default);
}
```

**Implementation:** `EventPublishingChatClient` in Orchestrations routes these to the host via the appropriate channel based on `AgentRole`.

---

## 5. SafetyEngine Abstractions

### ISafetyMiddleware

**File:** `SafetyEngine/ISafetyMiddleware.cs`

```csharp
public interface ISafetyMiddleware : IChatClient
{
    IChatClient InnerClient { get; }
    IReadOnlyList<ISafetyRule> Rules { get; }
}
```

### ISafetyRule

**File:** `SafetyEngine/ISafetyRule.cs`

```csharp
public interface ISafetyRule
{
    string RuleId { get; }
    string Name { get; }
    Task<SafetyResult> EvaluateAsync(SafetyContext context, CancellationToken ct = default);
}
```

### SafetyContext

**File:** `SafetyEngine/SafetyContext.cs`

```csharp
public sealed record SafetyContext
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public required ChatOptions? Options { get; init; }
    public required AgentRole AgentRole { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
```

### SafetyResult

**File:** `SafetyEngine/SafetyResult.cs`

```csharp
public sealed record SafetyResult
{
    public required SafetyVerdict Verdict { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string RuleId { get; init; } = string.Empty;
    public IReadOnlyList<ChatMessage>? ModifiedMessages { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();

    public static SafetyResult Allow(string ruleId, string reason = "Allowed") => new() { Verdict = SafetyVerdict.Allow, RuleId = ruleId, Reason = reason };
    public static SafetyResult Block(string ruleId, string reason) => new() { Verdict = SafetyVerdict.Block, RuleId = ruleId, Reason = reason };
    public static SafetyResult Modify(string ruleId, string reason, IReadOnlyList<ChatMessage> messages) => new() { Verdict = SafetyVerdict.Modify, RuleId = ruleId, Reason = reason, ModifiedMessages = messages };
    public static SafetyResult Flag(string ruleId, string reason, IReadOnlyDictionary<string, object?>? metadata = null) => new() { Verdict = SafetyVerdict.Flag, RuleId = ruleId, Reason = reason, Metadata = metadata ?? new Dictionary<string, object?>() };
}
```

### SafetyVerdict

**File:** `SafetyEngine/SafetyVerdict.cs`

```csharp
public enum SafetyVerdict
{
    Allow = 0,
    Block = 1,
    Modify = 2,
    Flag = 3
}
```

---

## 6. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Zero dependencies on other SentinelCore projects | ✅ | Pure abstractions/DTOs |
| All repository abstractions defined | ✅ | 4 interfaces |
| All case flow DTOs defined | ✅ | 8 record types |
| All enums defined | ✅ | CaseStatus, InvestigationSurface, AgentRole, OrchestrationType, SafetyVerdict |
| Settings class with all config | ✅ | SentinelCoreSettings |
| Events interface for host | ✅ | ISentinelCoreEvents (7 channels) |
| Safety abstractions complete | ✅ | Middleware, Rule, Context, Result, Verdict |
| Records are immutable | ✅ | All DTOs are `sealed record` |
| XML docs on all public types | ✅ | Comprehensive documentation |

---

## 7. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| `IEmbeddingService` abstraction | Abstractions | ❌ Not defined |
| `IContentSafetyClient` abstraction | SafetyEngine | ❌ Not defined |
| Safety rule configuration in Settings | SentinelCoreSettings | ❌ Not added |
| CaseStatus transition validation | CaseFlowEngine | ⚠️ Enforced in engine, not in contract |
| PatternMemory embedding dimension constant | CaseFlow | ⚠️ Hardcoded 1536 in multiple places |
| Event payload serialization contract | Events | ⚠️ Implicit JSON |

---

## 8. Related Documentation

| Document | Description |
|----------|-------------|
| `CaseFlowEngineComponent.md` | CaseFlowEngine implementation of repositories |
| `PersistenceComponent.md` | EF Core entities implementing these abstractions |
| `DynamicAgentsComponent.md` | AgentSpec, AgentRole, AgentPersona usage |
| `OrchestrationComponent.md` | OrchestrationType, ISentinelCoreEvents usage |
| `SafetyRailsComponent.md` | Safety middleware implementation |
| `MemoryLayerComponent.md` | IPatternMemoryStore, PatternMemory usage |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
