---
name: "Drift Prevention"
applyTo: "**"
description: "Details coding conventions and standards that must be followed for every task by every agent while working in this repo"
---

# SentinelCore Architectural Drift Prevention

Before modifying any `.cs` file in `SentinelCore`, read and follow the pattern-lock at
`/architecture/pattern-lock.md`. The rules below are a quick-reference summary — the
pattern-lock is authoritative.

**Pattern Lock Violations**
- If an agent finds lock violations take note of it and report to user in task summary even if violation is out of task scope.

## Layer Dependency Rules

Dependencies point **upward only**. A lower layer must never reference a higher layer.

| Layer                       | May depend on                                     |
| --------------------------- | ------------------------------------------------- |
| `Contracts/`                | Nothing (pure DTOs)                               |
| `Domain/Contracts/`         | Nothing (pure domain value types)                 |
| `Application/Abstractions/` | `Contracts/` only                                 |
| `CaseFlow/`                 | `Application.Abstractions` + `Domain.Contracts`   |
| `Agents/`                   | `Application` + `Contracts` + `Agents` subfolders |
| `Orchestration/`            | `Agents` + `Application` + `Infrastructure`       |
| `Infrastructure/DI/`        | All layers (wiring)                               |

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


Every agent must be constructed through `ISentinelAgentFactory` using 'AgentPresetDefinition'.
Agent factory provides overloads to override presets for instructions, tools, responseformat at the callsite for flexibility and allow agents to be reused for different purposes

- The Manager agent cannot use tools and should not be assigned any, this is a MAF orchestration limitation, group chat managers cannot use tools..
- Function invocation is handled by `ChatClientAgent` automatically — do not add
  `UseFunctionInvocation` to the agent builder pipeline.

## DI Anti-Patterns (Prohibited)

- ❌ `services.BuildServiceProvider()` inside registration methods
- ❌ `services.AddLogging()` inside the library — the host owns logging

## Nullable Analyzer Warnings

Do NOT introduce or edit code that creates a nullable warning without properly handling the nullable value. Resolve nullable warnings before completing task.

Apply attributes on methods to explicitly declare intentions


## UI/User Output

* All application and agent output flows through `ISystemReporter` which distributes to available loggers and application events. Never use ungated `Debug.WriteLine` or  `Console.WriteLine` it should be debug gated.
* Members tagged with `TemporaryAttribute` is code under review and should be left alone. It will be removed before shipping.
* The library must never reference the host project.

## Pre-Code Checklist

Before editing any `.cs` file, verify:

- [ ] Does the change add a `using` from a lower layer to a higher layer? If yes, stop.
- [ ] Does the change use `SentinelCore.*` (wrong root namespace)? Use `SentinelCore.*`.
- [ ] Does the change construct a `ChatClientAgent` directly? Use `ISentinelAgentFactory`.
- [ ] Does the change add `Console.WriteLine` in orchestration? Publish to `ISystemReporter`.
- [ ] Does the change duplicate an existing type? Each type has exactly one canonical location.
- [ ] Does the change throw from a Null\* implementation? Return a no-op result instead.
- [ ] Does the change add or modify an executor without `[YieldsOutput]`, `[MessageHandler]`, and `partial`? See PL-9.
- [ ] Does the change add an abstract base class for executors? Use convention + attributes instead. See PL-9.
- [ ] Does the change use `Executor<TIn, TOut>` (generic with type params) instead of `Executor` (non-generic)? See PL-9.
- [ ] Does the change use a primary constructor on an executor? Use conventional constructor instead. See PL-9.
- [ ] Does the change use `override HandleAsync` on an executor? Use `[MessageHandler]` on a named method instead. See PL-9.
- [ ] Does the change swallow `OperationCanceledException` in an executor? It must always propagate. See PL-9.
- [ ] Does the change throw past the executor boundary in the generic catch? Return a fallback instead. See PL-9.

If a change violates any locked pattern, stop state the change and justification for the change in chat and ask user for approval.

Use the `document-steward` agent to verify related documentation after edits and if an ADR is required for change.
