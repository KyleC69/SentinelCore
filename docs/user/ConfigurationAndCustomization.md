# SentinelCore Configuration & Customization Guide

> **Audience:** Developers integrating SentinelCore into a host application (WPF, console, web, test harness).
> **Last updated:** 2026-07-16

---

## Overview

SentinelCore is configured through two mechanisms:

1. **`SentinelCoreSettings`** — A POCO that controls runtime behavior: model endpoints, orchestration strategy, tracing, and persistence.
2. **`ISentinelCoreBuilder`** — A fluent builder that lets you opt into optional modules (magnetic orchestration, investigation control, persistence).

Both are passed to the single entry point:

```csharp
services.AddSentinelCore(settings, builder =>
{
    builder.AddMagneticOrchestration();
    builder.AddInvestigationControl();
    builder.AddSentinelCorePersistenceRepository(connectionString);
});
```

This document explains every property, every builder method, and the behavior you can expect from each combination.

---

## 1. SentinelCoreSettings Reference

`SentinelCoreSettings` is a `sealed class` in the `SentinelCore.Contracts.Contracts` namespace. It is the single configuration object the host passes to `AddSentinelCore`.

### 1.1 Model Configuration

Each agent role has its own `ModelSettings` property. This lets you assign different models (or the same model with different parameters) to different roles.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CoreModel` | `ModelSettings?` | `null` | Model for the Core investigative agent. If `null`, falls back to `DefaultModel`. |
| `ManagerModel` | `ModelSettings?` | `null` | Model for the Manager orchestration agent. If `null`, falls back to `DefaultModel`. |
| `DomainModel` | `ModelSettings?` | `null` | Model for Domain and Worker agents. If `null`, falls back to `DefaultModel`. |
| `DefaultModel` | `ModelSettings?` | `null` | Fallback model for any role that doesn't have a dedicated model configured. **Required** if any role-specific model is `null`. |

#### ModelSettings Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Endpoint` | `string` | `"http://127.0.0.1:11434"` | Ollama API endpoint URL. |
| `ModelId` | `string` | `""` | Ollama model identifier (e.g. `"llama3.2"`, `"mistral"`). **Required.** |
| `Temperature` | `float` | `0.1` | Sampling temperature. Lower = more deterministic, higher = more creative. Range: 0.0–2.0. |
| `MaxOutputTokens` | `int?` | `4000` | Maximum tokens in the model's response. |
| `TopK` | `int` | `1` | Top-K sampling parameter. Limits token selection to the K most probable tokens. |
| `TopP` | `float` | `1.0` | Nucleus sampling parameter. Limits token selection to the smallest set whose cumulative probability exceeds P. |

#### Model Resolution by Role

When `AgentSpecBuilder` constructs an agent spec, it resolves the model using this priority:

| Role | Resolution Order |
|------|-----------------|
| Core | `CoreModel` → `DefaultModel` → **throws** |
| Manager | `ManagerModel` → `DefaultModel` → **throws** |
| Domain | `DomainModel` → `DefaultModel` → **throws** |
| Worker | `DefaultModel` → **throws** |
| General | `DefaultModel` → **throws** |
| Aggregator | `DefaultModel` → **throws** |

**Minimum viable configuration:** Set `DefaultModel` with a valid `Endpoint` and `ModelId`. All roles will fall back to it.

**Recommended configuration for production:** Set `CoreModel`, `ManagerModel`, and `DomainModel` separately so you can tune temperature and token limits per role. The Core agent typically benefits from a lower temperature (0.0–0.2) for deterministic forensic analysis, while the Manager may benefit from a slightly higher temperature (0.3–0.5) for creative plan generation.

#### Example: Minimal Configuration

```csharp
var settings = new SentinelCoreSettings
{
    DefaultModel = new ModelSettings(
        endpoint: "http://localhost:11434",
        modelId: "llama3.2",
        temperature: 0.1f
    )
};
```

#### Example: Per-Role Configuration

```csharp
var settings = new SentinelCoreSettings
{
    CoreModel = new ModelSettings(
        endpoint: "http://localhost:11434",
        modelId: "llama3.2",
        temperature: 0.0f,       // Deterministic for forensic analysis
        maxOutputTokens: 8000
    ),
    ManagerModel = new ModelSettings(
        endpoint: "http://localhost:11434",
        modelId: "mistral",
        temperature: 0.3f,       // Slightly creative for plan generation
        maxOutputTokens: 4000
    ),
    DomainModel = new ModelSettings(
        endpoint: "http://localhost:11434",
        modelId: "llama3.2",
        temperature: 0.1f,       // Factual for evidence gathering
        maxOutputTokens: 4000
    ),
    DefaultModel = new ModelSettings(
        endpoint: "http://localhost:11434",
        modelId: "llama3.2",
        temperature: 0.2f
    )
};
```

### 1.2 Orchestration Strategy

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `OrchestrationType` | `OrchestrationType` | `OrchestrationType.Magnetic` | Determines which `ISentinelWorkflow` implementation is used when `IOrchestrationControl.InitializeOrchestrationAsync()` is called. |

#### OrchestrationType Values

| Value | Implementation | Status | Behavior |
|-------|---------------|--------|----------|
| `Magnetic` | `MagneticCoopOrchestration` | **In development** | Cooperative multi-agent workflow with a Manager that delegates tasks to Domain/Worker agents. |
| `Investigative` | `TheCoreOrchestration` | Active (legacy) | The Core agent creates an investigation plan and delegates execution to `MagneticOrchestration`. Requires `AddMagneticOrchestration()`. |
| `SingleAgent` | `SingleAgent` | Active | A single general-purpose agent processes the prompt directly. Simplest strategy. |
| `GroupConcurrent` | `GroupConcurrentOrchestration` | Active | Multiple agents run concurrently in a group workflow. |
| `GroupTurnBased` | `GroupTurnBasedOrchestration` | Stub | Reports "not implemented" via `ISystemReporter` and returns immediately. |
| `Sequential` | `SequentialOrchestration` | Stub | Reports "not implemented" via `ISystemReporter` and returns immediately. |

**What to expect:**

- **`SingleAgent`** — The fastest way to get started. One agent, one prompt, one response. No multi-agent coordination. Good for testing, simple queries, or when you don't need investigation workflows.
- **`Investigative`** — The full investigation pipeline. The Core agent analyzes the signal, creates a plan, and delegates to `MagneticOrchestration` for execution. Requires both `AddMagneticOrchestration()` and `AddSentinelCorePersistenceRepository()`.
- **`Magnetic`** — Direct magnetic cooperative orchestration without the Core planning loop. Currently in development — not recommended for production use.
- **`GroupConcurrent`** — Runs multiple agents concurrently. Useful for parallel evidence gathering across different domains.
- **`GroupTurnBased`** and **`Sequential`** — Stub implementations. They will report an error via `ISystemReporter` and return immediately without processing the prompt.

### 1.3 Tracing & Diagnostics

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TraceEnabled` | `bool` | `false` | When `true`, enables verbose agent trace logging through `LoggingChatClient`. |
| `TraceLogLevel` | `LogLevel` | `LogLevel.Trace` | The minimum log level for trace output. Only used when `TraceEnabled` is `true`. |

**What to expect:**

When `TraceEnabled` is `true`, every agent's chat client pipeline includes `LoggingChatClient`, which emits detailed trace logs at the configured level. This includes:

- Full prompt text sent to the model
- Model response text
- Tool call requests and results
- Middleware pipeline execution order

> **Note:** The library does **not** call `AddLogging()`. The host owns logging configuration. Trace logging uses whatever `ILoggerFactory` the host has registered.

### 1.4 Persistence

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SqlConnectionString` | `string?` | `null` | SQL Server connection string for the persistence layer. When `null`, persistence is not configured. |

**What to expect:**

- When `SqlConnectionString` is set and you call `builder.AddSentinelCorePersistenceRepository(connectionString)`, the library registers EF Core with SQL Server, `ICaseRepository`, `IEvidenceStore`, `IPatternMemoryStore`, and the real `ICaseFlowEngine`.
- When it's `null` or not provided, persistence is silently skipped. No error, no `BuildServiceProvider()` call.
- The connection string is also available through `SentinelCoreSettings.SqlConnectionString` for host-side reference.

### 1.5 Skills Directory (Deprecated)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SkillsDirectory` | `string` | `""` | **Deprecated.** Previously used for file-based skill definitions. Skills are now strongly-typed configuration classes. This property is retained for backward compatibility but has no effect. |

---

## 2. ISentinelCoreBuilder — Optional Modules

`ISentinelCoreBuilder` is the fluent interface for opting into optional SentinelCore modules. It is passed to the `configure` callback in `AddSentinelCore`.

### 2.1 Builder Methods

| Method | Description | Dependencies |
|-------|-------------|-------------|
| `AddMagneticOrchestration()` | Registers `IMagneticOrchestration` as a singleton. Required by `Investigative` orchestration type. | None |
| `AddInvestigationControl()` | Marks investigation control as enabled. Validates that magnetic orchestration and persistence are also enabled. | Requires `AddMagneticOrchestration()` and `AddSentinelCorePersistenceRepository()` |
| `AddSentinelCorePersistenceRepository(connectionString)` | Registers EF Core SQL Server, `ICaseRepository`, `IEvidenceStore`, `IPatternMemoryStore`, and the real `CaseFlowEngine`. Overrides the null-object `ICaseFlowEngine`. | Requires a non-empty connection string |

### 2.2 Builder Properties

| Property | Type | Description |
|----------|------|-------------|
| `Services` | `IServiceCollection` | The service collection being configured. Allows the host to register additional services during configuration. |
| `InvestigationControlEnabled` | `bool` | Whether investigation control has been enabled. Set by `AddInvestigationControl()`. |
| `MagneticOrchestrationEnabled` | `bool` | Whether magnetic orchestration has been enabled. Set by `AddMagneticOrchestration()`. |
| `PersistenceLayerEnabled` | `bool` | Whether the persistence layer has been enabled. Set by `AddSentinelCorePersistenceRepository()`. |

### 2.3 Dependency Validation

After the `configure` callback completes, `ValidateDependencies()` runs automatically. It throws `InvalidOperationException` with an actionable message if:

| Condition | Error Message |
|-----------|--------------|
| `AddInvestigationControl()` called without `AddMagneticOrchestration()` | "AddInvestigationControl() requires AddMagneticOrchestration() to also be called." |
| `AddInvestigationControl()` called without `AddSentinelCorePersistenceRepository()` | "AddInvestigationControl() requires AddSentinelCorePersistanceRepository() to also be called." |

This validation happens at configuration time, not at DI resolution time, so you get a clear error immediately.

---

## 3. Configuration Recipes

### 3.1 Minimal — Single Agent Only

The simplest configuration. No persistence, no investigation workflows. Just a single agent that processes prompts.

```csharp
var settings = new SentinelCoreSettings
{
    DefaultModel = new ModelSettings("http://localhost:11434", "llama3.2"),
    OrchestrationType = OrchestrationType.SingleAgent
};

services.AddSentinelCore(settings, builder =>
{
    // No optional modules needed
});
```

**What you get:**

- A single general-purpose agent processes each prompt.
- Events published to `ISentinelCoreEvents` for UI display.
- Errors reported through `ISystemReporter`.
- No case management, no persistence, no investigation workflows.

### 3.2 Full Investigation Pipeline

The complete configuration with case management, persistence, and magnetic orchestration.

```csharp
var settings = new SentinelCoreSettings
{
    CoreModel = new ModelSettings("http://localhost:11434", "llama3.2", temperature: 0.0f),
    ManagerModel = new ModelSettings("http://localhost:11434", "mistral", temperature: 0.3f),
    DomainModel = new ModelSettings("http://localhost:11434", "llama3.2", temperature: 0.1f),
    DefaultModel = new ModelSettings("http://localhost:11434", "llama3.2"),
    OrchestrationType = OrchestrationType.Investigative,
    TraceEnabled = true,
    TraceLogLevel = LogLevel.Debug,
    SqlConnectionString = "Server=.;Database=SentinelCore;Trusted_Connection=True;"
};

services.AddSentinelCore(settings, builder =>
{
    builder.AddMagneticOrchestration();
    builder.AddInvestigationControl();
    builder.AddSentinelCorePersistenceRepository(settings.SqlConnectionString!);
});
```

**What you get:**

- The Core agent creates an investigation plan from the signal.
- `MagneticOrchestration` executes the plan with Manager and Domain agents.
- Case lifecycle managed by `CaseFlowEngine` with SQL Server persistence.
- Evidence stored in the database via `IEvidenceStore`.
- Pattern memory available via `IPatternMemoryStore`.
- Full event stream for UI display.

### 3.3 Group Concurrent — Parallel Evidence Gathering

Multiple agents run concurrently to gather evidence from different domains.

```csharp
var settings = new SentinelCoreSettings
{
    DefaultModel = new ModelSettings("http://localhost:11434", "llama3.2"),
    OrchestrationType = OrchestrationType.GroupConcurrent
};

services.AddSentinelCore(settings, builder =>
{
    // No persistence or magnetic orchestration needed
});
```

**What you get:**

- Multiple agents run concurrently in a group workflow.
- Each agent processes a portion of the prompt.
- Results are aggregated and published to `ISentinelCoreEvents`.

### 3.4 With Tracing Enabled

Add trace logging to any configuration for debugging and development.

```csharp
var settings = new SentinelCoreSettings
{
    DefaultModel = new ModelSettings("http://localhost:11434", "llama3.2"),
    OrchestrationType = OrchestrationType.SingleAgent,
    TraceEnabled = true,
    TraceLogLevel = LogLevel.Debug
};

services.AddSentinelCore(settings, builder =>
{
    // Trace logging will be active for all agents
});
```

**What you get:**

- Every agent's full prompt, response, and tool calls are logged at the configured level.
- Useful for debugging agent behavior, verifying tool selection, and diagnosing model issues.
- **Warning:** Trace logging is verbose. Do not enable in production unless actively debugging.

---

## 4. Event Subscription

After configuration, subscribe to `ISentinelCoreEvents` in your host to receive agent activity:

```csharp
public class MyHostService
{
    private readonly ISentinelCoreEvents _events;

    public MyHostService(ISentinelCoreEvents events)
    {
        _events = events;
        _events.TheCoreActivity += OnCoreActivity;
        _events.MagnetManagerActivity += OnManagerActivity;
        _events.MagneticParticipantActivity += OnParticipantActivity;
        _events.MagneticWorkflowTooling += OnWorkflowTooling;
        _events.OrchestrationEvent += OnOrchestrationEvent;
        _events.ErrorOccurred += OnError;
    }

    private void OnCoreActivity(CoreActivityArgs args)
    {
        Console.WriteLine($"[Core:{args.AgentId}] {args.Output}");
    }

    private void OnManagerActivity(MagneticActivityArgs args)
    {
        Console.WriteLine($"[Manager:{args.AgentId}] {args.Output}");
    }

    private void OnOrchestrationEvent(OrchestrationActivityArgs args)
    {
        Console.WriteLine($"[{args.Source}] {args.Message}");
    }

    private void OnError(string message, Exception ex)
    {
        Console.Error.WriteLine($"ERROR: {message} — {ex.Message}");
    }

    // ... other handlers
}
```

### Event Channels Reference

| Event | Type | Raised By | Content |
|-------|------|-----------|---------|
| `TheCoreActivity` | `CoreActivityArgs` | Core agent | Core agent text output |
| `TheCoreReasoning` | `CoreActivityArgs` | Core agent | Core agent reasoning output |
| `MagnetManagerActivity` | `MagneticActivityArgs` | Manager agent + workflow events | Manager text output, plan events |
| `MagneticParticipantActivity` | `MagneticActivityArgs` | Domain/Worker agents | Domain/Worker agent text output |
| `MagneticWorkflowTooling` | `MagneticActivityArgs` | Manager/Domain/Worker agents | Tool results, progress |
| `OrchestrationEvent` | `OrchestrationActivityArgs` | All orchestrations | Lifecycle events (start, completion, errors) |
| `ErrorOccurred` | `(string, Exception)` | `SystemReporter` | Error messages and exceptions |

---

## 5. Starting an Investigation

After configuration, use `IOrchestrationControl` to start an investigation:

```csharp
public class InvestigationService
{
    private readonly IOrchestrationControl _control;

    public InvestigationService(IOrchestrationControl control)
    {
        _control = control;
    }

    public async Task StartAsync(string userPrompt, CancellationToken ct = default)
    {
        ChatMessage promptSignal = new(ChatRole.User, userPrompt);
        await _control.InitializeOrchestrationAsync(promptSignal, ct);
    }
}
```

The `OrchestrationType` in `SentinelCoreSettings` determines which orchestration strategy is used.

---

## 6. Common Pitfalls

### 6.1 Missing Model Configuration

**Problem:** `InvalidOperationException` at runtime: "CoreModel or DefaultModel must be configured."

**Fix:** Set at least `DefaultModel` in `SentinelCoreSettings`, or set each role-specific model:

```csharp
var settings = new SentinelCoreSettings
{
    DefaultModel = new ModelSettings("http://localhost:11434", "llama3.2")
};
```

### 6.2 Investigation Control Without Persistence

**Problem:** `InvalidOperationException` at configuration time: "AddInvestigationControl() requires AddSentinelCorePersistanceRepository() to also be called."

**Fix:** Always pair `AddInvestigationControl()` with `AddSentinelCorePersistenceRepository(connectionString)`:

```csharp
services.AddSentinelCore(settings, builder =>
{
    builder.AddMagneticOrchestration();
    builder.AddInvestigationControl();
    builder.AddSentinelCorePersistenceRepository(connectionString);
});
```

### 6.3 Investigative Orchestration Without Magnetic Orchestration

**Problem:** `InvalidOperationException` at configuration time: "AddInvestigationControl() requires AddMagneticOrchestration() to also be called."

**Fix:** `Investigative` orchestration requires `AddMagneticOrchestration()`:

```csharp
services.AddSentinelCore(settings, builder =>
{
    builder.AddMagneticOrchestration();
    builder.AddInvestigationControl();
    builder.AddSentinelCorePersistenceRepository(connectionString);
});
```

### 6.4 Using a Stub Orchestration Type

**Problem:** `GroupTurnBased` or `Sequential` orchestration types report "not implemented" and return immediately.

**Fix:** These are stub implementations. Use `SingleAgent`, `GroupConcurrent`, or `Investigative` for working orchestrations. `GroupTurnBased` and `Sequential` will be completed in future releases.

### 6.5 Ollama Not Running

**Problem:** `OllamaApiClient` throws an exception when trying to connect to the model endpoint.

**Fix:** Ensure Ollama is running and the `Endpoint` in `ModelSettings` is correct. The default is `http://127.0.0.1:11434`. Verify with:

```bash
curl http://127.0.0.1:11434/api/tags
```

### 6.6 Temperature Out of Range

**Problem:** Model produces unexpected or repetitive output.

**Fix:** Adjust `Temperature` in `ModelSettings`:

- **0.0–0.1:** Deterministic, factual output. Best for forensic analysis and evidence gathering.
- **0.2–0.5:** Balanced. Good for plan generation and creative reasoning.
- **0.6–1.0:** Creative, varied output. Useful for brainstorming but may produce inconsistent results.
- **Above 1.0:** Highly random. Not recommended for investigation workflows.

---

## 7. Quick Reference

### Always-On Services

These services are registered unconditionally by `AddSentinelCore()`:

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `ISentinelCoreEvents` | `SentinelCoreEvents` | Singleton |
| `IAgentBuilder` | `AgentBuilder` | Singleton |
| `IAgentSpecBuilder` | `AgentSpecBuilder` | Singleton |
| `ICoreAgentFactory` | `CoreAgentFactory` | Singleton |
| `IManagerAgentFactory` | `ManagerAgentFactory` | Singleton |
| `IDomainAgentFactory` | `DomainAgentFactory` | Singleton |
| `IOrchestrationFactory` | `OrchestrationFactory` | Singleton |
| `IOrchestrationControl` | `OrchestrationControl` | Singleton |
| `ISystemReporter` | `SystemReporter` | Singleton |
| `TheCoreOrchestration` | `TheCoreOrchestration` | Singleton |
| `SingleAgent` | `SingleAgent` | Singleton |
| `GroupConcurrentOrchestration` | `GroupConcurrentOrchestration` | Singleton |
| `GroupTurnBasedOrchestration` | `GroupTurnBasedOrchestration` | Singleton |
| `SequentialOrchestration` | `SequentialOrchestration` | Singleton |
| `MagneticCoopOrchestration` | `MagneticCoopOrchestration` | Singleton |
| `MagneticOrchestration` | `MagneticOrchestration` | Singleton |

### Optional Services

These services are registered by builder methods:

| Builder Method | Services Registered |
|---------------|-------------------|
| `AddMagneticOrchestration()` | `IMagneticOrchestration` → `MagneticOrchestration` (Singleton) |
| `AddSentinelCorePersistenceRepository(connStr)` | `SentinelCoreDBContext`, `ICaseRepository` → `CaseRepository`, `IEvidenceStore` → `EvidenceStore`, `IPatternMemoryStore` → `PatternMemoryStore`, `ICaseFlowEngine` → `CaseFlowEngine` (overrides null-object) |
| `AddInvestigationControl()` | Marks investigation control as enabled (validates dependencies) |
