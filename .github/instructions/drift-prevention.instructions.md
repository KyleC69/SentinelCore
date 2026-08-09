---
description: "Use when writing, modifying, or reviewing C# source files in SentinelCore. Provides layer dependency rules, namespace conventions, agent construction pipeline rules, DI anti-patterns, and the pattern-lock change checklist to prevent architectural drift."
applyTo: "**/*.cs"
---

# SentinelCore Architectural Drift Prevention

Before modifying any `.cs` file in `SentinelCore`, read and follow the pattern-lock at
`/architecture/pattern-lock.md`. The rules below are a quick-reference summary — the
pattern-lock is authoritative.

## Layer Dependency Rules

Dependencies point **upward only**. A lower layer must never reference a higher layer.

| Layer | May depend on |
|-------|--------------|
| `Contracts/` | Nothing (pure DTOs) |
| `Domain/Contracts/` | Nothing (pure domain value types) |
| `Application/Abstractions/` | `Contracts/` only |
| `CaseFlow/` | `Application.Abstractions` + `Domain.Contracts` |
| `Agents/` | `Application` + `Contracts` + `Agents` subfolders |
| `Orchestration/` | `Agents` + `Application` + `Infrastructure` |
| `Infrastructure/DI/` | All layers (wiring) |

### Forbidden `using` directives

```text
Contracts → Application          ❌
Contracts → Orchestration        ❌
Domain → Application             ❌
Domain → Agents                  ❌
Domain → Orchestration           ❌
CaseFlow → Agents                ❌
CaseFlow → Orchestration         ❌
Agents → Orchestration           ❌
```

## Namespace Convention

Always use `SentinelCore.*` as the root namespace.
The namespace must match the folder path relative to the project root.

## Agent Construction
**Under Review subject to change**
Every agent must be constructed through `IAgentBuilder.Build(AgentSpec)`.
Never construct `ChatClientAgent` directly in a factory.

- Every factory produces an `AgentSpec` and delegates to `IAgentBuilder`.
- `AgentRole` determines event routing and middleware — do not add roles without
  updating `EventPublishingChatClient` and `AgentBuilder`.
- `SafetyMiddleware` and `PatternMemoryInjector` are applied **only** to the Core agent.
- The Manager agent must not have tools.
- Function invocation is handled by `ChatClientAgent` automatically — do not add
  `UseFunctionInvocation` to the agent builder pipeline.

## DI Anti-Patterns (Prohibited)

- ❌ `services.BuildServiceProvider()` inside registration methods
- ❌ `services.AddLogging()` inside the library — the host owns logging
- ❌ Registering `ISentinelCoreBuilder` as a DI service
- ❌ Registering `ICaseFlowEngine` unconditionally (depends on optional persistence)
- ❌ Registering `ISentinelCoreBuilder` in the DI container

## Null-Object Pattern

Every optional module has a `Null*` default. Null implementations must never throw
`NotImplementedException` or return `Task.FromCanceled`. They return `Task.CompletedTask`
or default values.

Builder methods override null defaults using `RemoveAll<T>() + AddSingleton<T>()`.

## Event Publishing

All agent output flows through `ISentinelCoreEvents`. Never use `Console.WriteLine`
for agent or workflow output. The library must never reference the host project.

## Pre-Code Checklist

Before editing any `.cs` file, verify:

- [ ] Does the change add a `using` from a lower layer to a higher layer? If yes, stop.
- [ ] Does the change use `SentinelCore.*` (wrong root namespace)? Use `SentinelCore.*`.
- [ ] Does the change construct a `ChatClientAgent` directly? Use `IAgentBuilder.Build()`.
- [ ] Does the change add `Console.WriteLine` in orchestration? Publish to `ISentinelCoreEvents`.
- [ ] Does the change duplicate an existing type? Each type has exactly one canonical location.
- [ ] Does the change throw from a Null* implementation? Return a no-op result instead.

If a change violates any locked pattern, create an ADR in `/docs/decisions/` before
proceeding. Use the `docs-steward` agent or the `/create-adr` prompt.
