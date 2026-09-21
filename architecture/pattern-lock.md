---
title: "Pattern Lock"
status: Active
component: Architecture
last_updated: 2026-09-10
---

# SentinelCore Pattern Lock

This is the **authoritative** lock on SentinelCore's architectural patterns.
The quick-reference rules in `.github/instructions/drift-prevention.instructions.md`
summarize this file — when the two disagree, this file wins.

Every locked pattern carries an ID (`PL-#`). Any change that breaks a locked
pattern requires an ADR in `/docs/decisions/` (see `.github/prompts/create-adr.prompt.md`)
**before** the change lands. Changes that add or reinforce a pattern must update
this file in the same commit.

## PL-1: Layer Dependencies

Dependencies point **upward only**. A lower layer must never reference a higher layer.

| Layer | May depend on |
| ------- | -------------- |
| `Contracts/` | Nothing (pure DTOs) |
| `Domain/Contracts/` | Nothing (pure domain value types) |
| `Application/Abstractions/` | `Contracts/` only |
| `CaseFlow/` | `Application.Abstractions` + `Domain.Contracts` |
| `Agents/` | `Application` + `Contracts` + `Agents` subfolders |
| `Orchestration/` | `Agents` + `Application` + `Infrastructure` |
| `Infrastructure/DI/` | All layers (wiring) |

Forbidden `using` directives:

```text
Contracts → Application          ❌
Contracts → Orchestration        ❌
Domain → Application             ❌
Domain → Agents                  ❌
Domain → Orchestration           ❌
CaseFlow → Agents                ❌
CaseFlow → Orchestration        ❌
Agents → Orchestration           ❌
```

## PL-2: Namespace Convention

Always use `SentinelCore.*` as the root namespace. The namespace must match the
folder path relative to the project root.

## PL-3: Agent Construction

**Under Review subject to change.**

Every agent must be constructed through `IAgentBuilder.Build(AgentSpec)`. Never
construct `ChatClientAgent` directly in a factory.

- Every factory produces an `AgentSpec` and delegates to `IAgentBuilder`.
- `AgentRole` determines event routing and middleware — do not add roles without
  updating `EventPublishingChatClient` and `AgentBuilder`.
- `SafetyMiddleware` and `PatternMemoryInjector` are applied **only** to the Core agent.
- The Manager agent must not have tools.
- Function invocation is handled by `ChatClientAgent` automatically — do not add
  `UseFunctionInvocation` to the agent builder pipeline.

## PL-4: DI Anti-Patterns

- ❌ `services.BuildServiceProvider()` inside registration methods
- ❌ `services.AddLogging()` inside the library — the host owns logging
- ❌ Registering `ISentinelCoreBuilder` as a DI service
- ❌ Registering `ICaseFlowEngine` unconditionally (depends on optional persistence)
- ❌ Registering `ISentinelCoreBuilder` in the DI container

## PL-5: Null-Object Pattern

Every optional module has a `Null*` default. Null implementations must never throw
`NotImplementedException` or return `Task.FromCanceled`. They return
`Task.CompletedTask` or default values.

Builder methods override null defaults using `RemoveAll<T>() + AddSingleton<T>()`.

## PL-6: Event Publishing

All agent output flows through `ISentinelCoreEvents`. Never use `Console.WriteLine`
for agent or workflow output. The library must never reference the host project.

## PL-7: EF Core DbContext Registration & Usage — Factory Pattern Standard

**Status:** Locked · **Date:** 2026-09-10 · **Enforced by:** `SentinelCore.Tests/Architecture/`

### Approved pattern

All EF Core `DbContext` registration and consumption in SentinelCore uses the
**factory pattern**. Registration types and consumption types must stay aligned.

**Registration (composition roots only):**

```csharp
// AddDbContextFactory registers BOTH IDbContextFactory<TContext> (singleton) and
// TContext itself (scoped). The scoped registration exists for infrastructure-internal
// use only — services must still consume the factory.
services.AddDbContextFactory<SentinelCoreDBContext>(options =>
{
 options.UseSqlServer(connectionString);
});
```

**Consumption (services):**

```csharp
public sealed class EvidenceStore : IEvidenceStore
{
 private readonly IDbContextFactory<SentinelCoreDBContext> _dbContextFactory;

 public EvidenceStore(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
 {
  _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
 }

 public async Task AddAsync(...)
 {
  // One short-lived context per operation — created from the factory,
  // disposed with await using.
  await using SentinelCoreDBContext db =
    await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
  ...
  await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
 }
}
```

### Rules

1. **Register with `AddDbContextFactory<TContext>`** in the host's composition root.
   `AddPooledDbContextFactory<TContext>` is an approved variant for approved hot
   paths (requires an ADR).
2. **Consume via `IDbContextFactory<TContext>` injection.** Every persistence
   service (`CaseFlowEngine`, `EvidenceStore`, `PatternMemoryStore`,
   `SignalRepository`) takes the factory in its constructor.
3. **One context per operation.** Create the context inside the method with
   `CreateDbContextAsync` and dispose it with `await using`. Never store a
   `DbContext` in a field, cache, or long-lived object.
4. **Registration/consumption alignment is mandatory.** A type injecting
   `IDbContextFactory<TContext>` requires a matching `AddDbContextFactory<TContext>`
   registration. `AddDbContext<TContext>` alone never registers the factory and
   causes `InvalidOperationException: Unable to resolve service for type
   'IDbContextFactory`1[...]'` at activation time.

### Prohibited

- ❌ `AddDbContext<TContext>` registrations (no factory is registered).
- ❌ `AddDbContextPool<TContext>` registrations (registers the pool, not the factory).
- ❌ Injecting the scoped `TContext` directly into any service. The WPF/agent host
  resolves services from the root provider, so a "scoped" context captured by a
  singleton/transient service becomes a process-wide shared instance — and
  `DbContext` is not thread-safe (captive dependency).
- ❌ Holding a `DbContext` beyond a single operation.

### Exemptions

- Auto-generated EF Core Power Tools partials in
  `projects/SentinelCore.CaseFlowEngine/Persistence/` (the context classes and
  their generated procedure/function partials).
- `projects/SentinelCore.CaseFlowEngine/Migrations/` (scaffolded migrations).
- Design-time factories implementing `IDesignTimeDbContextFactory<T>`.

### Rationale

The agent host runs concurrent workflows, chat, and UI on shared thread pools.
Factory-created short-lived contexts per operation are the only `DbContext`
lifetime that is safe under that concurrency, and it is the pattern
`CaseFlowEngine` already used. Mixed lifetimes (scoped injection + factory)
allowed drift that broke activation at runtime.

### Enforcement

Drift from this pattern fails the build in `SentinelCore.Tests`:

- `Architecture/DbContextPatternLockTests.cs` — scans solution source for banned
  registrations (`AddDbContext<`, `AddDbContextPool<`), direct context capture
  (`private readonly SentinelCoreDBContext`, `(... SentinelCoreDBContext name ...)`
  constructor parameters), and missing `AddDbContextFactory<TContext>` registrations.
- `Architecture/PersistenceRegistrationTests.cs` — builds the DI container the way
  the host does and asserts every persistence service resolves; fails when a
  factory consumer lacks its `AddDbContextFactory` registration; verifies via
  reflection and strict mocks that each persistence service constructs its context
  from the factory.

## PL-8: WPF UI Layer Patterns

**Status:** Locked · **Date:** 2026-09-10 · **Enforced by:** review + `SentinelCore.Tests` UI view-model tests

### PL-8.1: Style Triggers vs. Local Values (Dependency-Property Precedence)

WPF dependency-property precedence puts **local values above style triggers**.
Setting a property directly on an element (e.g. `Background="..."` on a `Border`)
permanently defeats every `Setter` in `Style.Triggers` for that property — the
trigger silently never applies.

**Approved pattern:** all visual state for templated/styled elements lives in the
`Style` (or `ControlTemplate.Triggers`); the element itself carries only
structure (margins, padding, layout).

```xml
<!-- ❌ Drift: local Background/BorderBrush defeat the DataTriggers below -->
<Border Background="{DynamicResource ChatSurface2}" BorderBrush="{DynamicResource ChatBorder}">
    <Border.Style>
        <Style TargetType="Border">
            <Style.Triggers>
                <DataTrigger Binding="{Binding Role.Value}" Value="user">
                    <Setter Property="Background" Value="{DynamicResource ChatUserBubble}" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
</Border>

<!-- ✅ Locked: default values are Style setters; triggers can override them -->
<Border>
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="Background" Value="{DynamicResource ChatSurface2}" />
            <Setter Property="BorderBrush" Value="{DynamicResource ChatBorder}" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding Role.Value}" Value="user">
                    <Setter Property="Background" Value="{DynamicResource ChatUserBubble}" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
</Border>
```

### PL-8.2: Binding to Microsoft.Extensions.AI Types

`ChatRole` exposes roles as **static** properties (`ChatRole.User`,
`ChatRole.Assistant`, `ChatRole.System`, `ChatRole.Tool`). WPF `{Binding}` paths
resolve **instance** properties only — `{Binding Role.User}` never works.
The instance property is `ChatRole.Value` (a lowercase string: `"user"`,
`"assistant"`, `"system"`, `"tool"`).

**Approved pattern:** bind `{Binding Role.Value}` and compare against the
lowercase string in `DataTrigger Value="..."`.

### PL-8.3: Bound Collection Mutation (Never Reassign)

`ObservableCollection<T>` properties bound to selection-aware controls
(`DataGrid.SelectedItem`, `ListBox.SelectedItem`) must be **mutated in place**
(`Clear()` + `Add()`), never reassigned (`Servers = new(...)`). Reassignment
resets the bound `SelectedItem` to `null`, silently collapsing detail panes
and losing user context after every refresh.

**Approved pattern:**

```csharp
// ✅ Locked: mutate; selection is restored by re-finding the selected id
Servers.Clear();
foreach (McpServerRow row in rows)
{
    Servers.Add(row);
}
SelectedServer = Servers.FirstOrDefault(s => s.Id == selectedId);
```

### PL-8.4: View-Model Lifecycle Ownership

`NavigationService` is the **single owner** of page/view-model teardown:

1. On navigate-away it raises `INavigationAware.OnNavigatedFrom()`.
2. It then disposes the outgoing page's view-model when it implements
   `IDisposable` (cancelling lifecycle CTSs, unsubscribing shared events).
3. Pages must **not** dispose their view-models in `Unloaded` — double
   disposal paths cause `ObjectDisposedException` races.
4. The `Frame` navigation journal is cleared after every hop (tab-style
   navigation); transient pages must never accumulate in the back stack.

### PL-8.5: Theme Brush Discipline

- All colors resolve through `{DynamicResource ...}` keys prefixed `Chat` in
  `Styles/ChatBrushes.xaml`. **Hardcoded hex values in views are drift.**
- New semantic shades are added as `Chat`-prefixed keys to `ChatBrushes.xaml`
  and reused (e.g. `ChatStatusSuccess`, `ChatStatusWarning`,
  `ChatStatusInfo`, `ChatStatusAttention`, `ChatStatusCritical`,
  `ChatCodeBackground`, `ChatFocusBorder`).
- Interactive styles must set a visible `FocusVisualStyle` — never
  `FocusVisualStyle="{x:Null}"` on keyboard-reachable controls. Use the shared
  `ChatFocusVisual` from `CaseStyles.xaml`.
- Control chrome that ships with a light default template (ComboBox, ScrollBar)
  must be re-templated in `CaseStyles.xaml`, not left with light Aero defaults
  on the dark palette.

### PL-8.6: UI Query Patterns (N+1 Prevention)

List/count UIs must use the single grouped query
(`ICaseFlowEngine.GetCaseStatusCountsAsync`) rather than looping
`GetCaseCountByStatusAsync` per status. Case lookups use
`GetCaseByIdAsync`; advance-target lists use `GetAllowedTransitions` so the UI
only ever offers legal lifecycle transitions.

## PL-9: Workflow Executor Convention

**Status:** Locked · **Date:** 2026-09-21 · **Enforced by:** `ExecutorTemplate.cs`

All workflow executors in `SentinelCore.Orchestrations.Workflows.Executors` must
follow the convention pattern defined in `ExecutorTemplate.cs`. There is **no
abstract base class** — the pattern is enforced by convention and compile-time
attributes, not by inheritance.

### Mandatory attributes

Every executor class **must** carry these MAF source-generator attributes for
compile-time validation:

| Attribute | Purpose | Where |
| ----------- | --------- | ------- |
| `[YieldsOutput(typeof(TOut))]` | Declares the output type so the MAF source generator validates that the workflow graph wires this executor's output to a compatible input. | On the class declaration. Add one per output type if the executor yields multiple types. |
| `[MessageHandler]` | Marks the handler method as the MAF dispatch entry point. The source generator uses this to build routing tables. | On the handler method (e.g., `HandleChatMessageAsync`). |
| `partial` keyword | Required by the MAF source generator for any class that carries `[YieldsOutput]` or `[MessageHandler]`. | On the class declaration. |

### Class declaration pattern

Every executor **must** extend `Executor` (non-generic, no type parameters). The
`[YieldsOutput]` and `[MessageHandler]` attributes provide the type information that
the generic `Executor<TIn, TOut>` base class previously carried.

```csharp
// ✅ Locked: non-generic Executor with attributes
[YieldsOutput(typeof(SignalHypothesis))]
public sealed partial class MyExecutor : Executor
{
    [MessageHandler]
    public async ValueTask<SignalHypothesis> HandleSignalHypothesisAsync(
        SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = default)
    { ... }
}

// ❌ Drift: generic Executor with type params
public sealed partial class MyExecutor : Executor<SignalHypothesis, SignalHypothesis>
{
    public override ValueTask<SignalHypothesis> HandleAsync(...) { ... }
}
```

### Handler method naming

The handler method **must** follow the `Handle{TIn}Async` naming convention:

| Input type | Handler method name |
| ----------- | --------------------- |
| `ChatMessage` | `HandleChatMessageAsync` |
| `SignalHypothesis` | `HandleSignalHypothesisAsync` |
| `string` | `HandleStringAsync` |

The handler method **must NOT** be `override`. The MAF source generator produces
the `HandleAsync` override automatically from the `[MessageHandler]` attribute.

### Constructor pattern

Every executor **must** use a conventional constructor (not a primary constructor).
Primary constructors cause issues with the MAF source generator.

```csharp
// ✅ Locked: conventional constructor
public sealed partial class MyExecutor : Executor
{
    private readonly ISystemReporter _reporter;
    public string Name { get; init; }

    public MyExecutor(ISystemReporter reporter) : base("MyExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }
}

// ❌ Drift: primary constructor
public sealed partial class MyExecutor(ISystemReporter reporter) : Executor<ChatMessage, SignalHypothesis>("MyExecutor")
```
