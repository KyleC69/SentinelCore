## Architecture Decision Record: Call-Site Agent Toolset Selection (ADR-003)

**Status:** Accepted
**Date:** 2026-02-14
**Architects:** [Agent/Copilot]
**Impacted Components:** `SentinelCore.Orchestrations` (`Abstractions/`, `Agents/`, `Infrastructure/DI/`, `Workflows/`), `SentinelCore.Tests`

---

### 1. Context and Problem Statement

Before this decision, every agent created by `ISentinelAgentFactory` received its toolset from a
single hard-coded rule inside `SentinelAgentFactory.BuildAgent`:

```csharp
Tools = preset.UseTools ? _builtToolCache.ToList() : []
```

`_builtToolCache` is populated exclusively from `IMcpServerRegistry.GetToolsFromCache()`. Three
problems followed from that:

1. **No call-site control.** A call site could ask for "tools" or "no tools", but could not say
   *which* tools. Every tool-bearing agent received the entire MCP catalog, which grows with every
   MCP server the host starts. A worker that only needs to write a report still saw every registry,
   WMI, and SQL tool the host had loaded.
2. **No local tools were reachable at all.** Deterministic in-process tools
   (`CaseEngineTools`, `MsDocsMcpServerTool`) could not be bound to an agent by any path. The only
   tools that ever reached a model call were MCP-server tools.
3. **A hard dependency on at least one MCP server.** `BuildAgent` called
   `Throw.IfNullOrEmpty(_builtToolCache.ToList())` unconditionally, so a host that had not yet
   started an MCP server could not construct *any* agent, including the many agents that need no
   tools at all.

The goal was therefore: let the call site choose the toolset, from the union of MCP-server tools and
local in-process `AIFunction` tools, without weakening the architecture rules in
[../../architecture/pattern-lock.md](../../architecture/pattern-lock.md).

### 2. Decision

Introduce a first-class selection value, `AgentToolSelection`, and thread it through
`ISentinelAgentFactory` → `CreateAgentCoreAsync` → `BuildAgent` → `ResolveTools`.

#### 2.1 Tool binding must happen at construction time

`ChatClientAgentRunOptions.ChatOptions` **unions** its `Tools` collection with the agent-level
`Tools` collection. A per-invocation tool list can therefore only *add* tools; it can never remove
them. Selecting a toolset per invocation is impossible by construction. The selection must be
applied when the `ChatClientAgent` is built, and the new API reflects that: the selection is a
`CreateAgentAsync` parameter, not a `RunAsync` option.

#### 2.2 The selection type

`SentinelCore.Orchestrations.Abstractions.AgentToolSelection` is a sealed record with three
strategies, exposed as static factories:

| Member | Resolves to |
| ------ | ----------- |
| `AgentToolSelection.None` | `[]` — the agent is bound no tools. |
| `AgentToolSelection.All` | Every tool in the merged MCP + local catalog. |
| `AgentToolSelection.FromNames(...)` | Exactly the named tools, resolved case-insensitively against the merged catalog. |

`FromNames` de-duplicates case-insensitively and rejects null, empty, or whitespace names, so a bad
selection fails at the call site rather than silently binding fewer tools than intended.

#### 2.3 Backwards compatibility

Every new overload takes `AgentToolSelection` in addition to the existing parameters; the five
original overloads are unchanged. A `null` selection means **preset default**: the factory binds the
tool groups the preset declares in `AgentPresetDefinition.ToolGroups`. (ADR-005 replaced the original
"preset alone decides" rule, which depended on a `preset.UseTools` flag that no longer exists, and
ADR-006 then replaced ADR-005's role toolset with preset-declared groups.) Existing call sites
therefore keep their exact previous semantics for every preset whose groups cover the same tools.

#### 2.4 Resolution rules

`ResolveTools` implements the following precedence:

* `null` → the preset's declared tool groups (see ADR-006); empty when the preset declares none.
* `None` → empty toolset.
* `All` → every tool carrying a registered `sc_<group>_` prefix; uncategorized tools are excluded.
* `Named` → the resolved subset, in the requested order; unknown names are logged and skipped.
* `Groups` → the union of tools claimed by the supplied groups' prefixes; an unknown group throws.

The merged catalog (`BuildToolCatalog`) applies an asymmetric collision policy:

* **MCP tool name collides with another MCP tool name** → log a warning and keep the first
  registration. Two external MCP servers advertising the same tool name is a deployment
  configuration issue, and must not abort a workflow.
* **Local tool name collides with anything** → throw `InvalidOperationException`. A local tool name
  is written by a developer; a collision is a defect and must fail fast.

A named tool that cannot be resolved throws `InvalidOperationException` listing both the missing
names and the available catalog, so the call site can be corrected immediately.

#### 2.5 Local tool catalog

Local tools are contributed through DI as `AITool` services and injected into the factory as
`IEnumerable<AITool>`. `SentinelCoreServiceExtensions.RegisterLocalAgentTools` registers:

| Tool | Source |
| ---- | ------ |
| `MsDocsMcpServerTool` | existing `AIFunction` implementation |
| `sc_internal_case_create` | `AIFunctionFactory.Create` over `CaseEngineTools.CreateCase` |
| `sc_internal_case_insert_report` | `AIFunctionFactory.Create` over `CaseEngineTools.InsertReport` |
| `sc_internal_case_insert_pattern_summaries` | `AIFunctionFactory.Create` over `CaseEngineTools.InsertPatternSummaries` |

Adding a local tool is therefore a one-line DI registration with no change to the factory. Note
that `RagSearchTool`, `MicrosoftDocsSearchTool`, `MicrosoftDocsFetchTool` and
`MicrosoftCodeSampleSearchTool` are **not** registered: they are non-invocable `AITool` stubs that
override neither `InvokeCoreAsync` nor `JsonSchema`, so binding them to an agent would hand the
model a tool it cannot call.

#### 2.6 Architectural enforcement stays structural

`EnsureManagerHasNoTools` throws when `AgentPreset.ManagerAgent` is given a non-empty toolset. The
check takes the `AgentPreset` enum, not the mutable `AgentId` string, so a renamed or cloned preset
cannot bypass PL-3. The Manager's "no tools" invariant is now enforced by the factory rather than by
call-site discipline.

#### 2.7 Availability change

`Throw.IfNullOrEmpty(_builtToolCache.ToList())` was replaced with a scoped warning:
construction proceeds with an empty toolset when nothing resolved, and a warning records the agent
name plus the MCP and local tool counts. Agent construction no longer requires a started MCP server.

#### 2.8 Defect fixed in the same change

The pre-existing three-argument overload
`CreateAgentAsync(preset, agentName, agentDescription)` accepted a name and description and then
silently dropped them, so every sub-workflow agent built through it shared the preset's default
name. It now assigns `presetDefinition.AgentName` and `presetDefinition.Description`. This was fixed
because the new overload set is defined in terms of call-site identity, and leaving the identity
hole in place would have made the new API misleading.

### 3. Alternatives Considered

1. **Bind tools per invocation through `ChatClientAgentRunOptions.ChatOptions`.**
   Rejected: the framework unions run options with agent options, so this can only add tools. An
   agent built with the full MCP catalog cannot be narrowed at run time.

2. **A `bool useTools` flag on every overload.**
   Rejected: it is exactly the capability that already existed and is the root of the problem. It
   cannot express "these three tools" or "the local tools only".

3. **A predicate / filter delegate (`Func<AITool, bool>`) instead of names.**
   Rejected: it makes the selection opaque to logging, diagnostics, and the workflow definition, and
   it invites arbitrary logic into what should be a declarative, reviewable choice. Names are stable,
   serialisable, and appear verbatim in error messages.

4. **Bind the whole merged catalog everywhere and rely on tool-choice behaviour.**
   Rejected: it inflates every prompt with irrelevant tool schemas, increases token cost, and
   measurably degrades tool selection accuracy. It also defeats the purpose of having distinct
   domain agents.

5. **Pass tools through the `AgentSession`.**
   Rejected: sessions carry conversation state, not construction configuration, and the Manager
   session is shared with its subagents — the toolset would leak across agents.

### 4. Consequences

**Positive**

* A call site can bind MCP tools, local tools, or both, by name or wholesale.
* Agents can be created with no MCP server running.
* PL-3 (no tools on the Manager) is now enforced in code.
* Local tools are additive-only from the factory's perspective: a new tool is a DI registration.
* Tool resolution failures are loud, actionable, and name the available catalog.

**Negative / accepted costs**

* **Overload ambiguity with a bare `null` literal.** `CreateAgentAsync(preset, null)` is now
  ambiguous between `string?` and `AgentToolSelection`. Call sites must cast
  (`(string?)null` or `(AgentToolSelection?)null`). No existing call site passes a bare `null`, and
  the ambiguity is caught at compile time, not at run time.
* **The factory's constructor signature changed** from `CaseEngineTools` to `IEnumerable<AITool>`.
  This is a breaking change for any external host that constructs `SentinelAgentFactory` directly;
  in-repo call sites and tests were updated.
* `AgentToolSelection.All` still exposes the full catalog to whichever agent asks for it. Call sites
  are responsible for choosing `Named` when a narrower toolset is appropriate.

**Known gaps (deliberately not addressed here)**

* `CreateAgentCoreAsync` still accepts `taskInstructions` and drops it; instructions are set from the
  preset only. Pre-existing, out of scope, and recorded here so it is not lost.
* `RagMiddlewarePipeline` is dead code and the RAG/doc `AITool` stubs are non-invocable. Neither is
  wired into the catalog; making them usable is a separate piece of work.

### 5. Verification Strategy

* `SentinelCore.Tests/AgentFactoryToolTests.cs` asserts observable behaviour through the
  `FakeChatClient`'s recorded `ChatOptions`, not through factory internals:
  * a `null` selection binds the preset's declared tool groups and never the whole catalogue;
  * `None` binds zero tools;
  * `All` binds every tool carrying a registered prefix and excludes uncategorized tools;
  * `FromNames` binds exactly one tool and resolves the name case-insensitively;
  * an unknown name logs a warning, is skipped, and construction still succeeds;
  * an unknown group name throws and names the missing group;
  * a group that claims no live tool yields an empty toolset without failing;
  * `ManagerAgent` with a non-empty toolset throws;
  * construction succeeds with an empty MCP catalog;
  * a call-site name and description reach the constructed agent;
  * `FromNames` and `FromGroups` collapse duplicates case-insensitively.
* `dotnet build SentinelCore.slnx` must report zero errors.
* Workflow call sites in `TheCoreWorkflow`, `CustomGroupWorkflow`, `ConcurrentGroupTeam`,
  `TheCoreSwitchExec` and `SignalClassifierExecutor` pass an `AgentToolSelection` only where the
  preset default is wrong for that agent. Everywhere else the preset's declared `ToolGroups` is the
  statement of intent, so the assignment lives in one place rather than being repeated per call site.
