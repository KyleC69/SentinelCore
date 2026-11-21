---
title: "Tooling Component"
status: Active
component: Tooling
last_updated: 2026-11-19
version: v4.1
---

# SentinelCore Tooling Component

**Project:** `SentinelCore.Orchestrations`
**Namespace:** `SentinelCore.Orchestrations.Abstractions`
**Dependencies:** `SentinelCore.Contracts` (MCP contracts, `AITool`), `Microsoft.Extensions.AI`
**Consumers:** `SentinelAgentFactory` (tool resolution), `TheCoreWorkflow`, `ConcurrentGroupTeam`, `CustomGroupWorkflow`
**Governing decisions:** [ADR-006](ADR/006-Tool-Group-Prefix-Scheme.md) (prefix scheme), [ADR-007](ADR/007-MsLearn-Knowledge-Tools-Temporary.md) (temporary MS Learn knowledge tools)

---

## Purpose

The MCP server (SentinelCore-MCP) supplies the read-only operating-system interrogation surface.
The Orchestrations project supplies a small set of in-process `AIFunction` tools for mutation.

**Key principles:**

- **Read-only evidence gathering.** Tools are **read-only** — they gather evidence, they don't
  mutate system state. Mutation is handled by the internal local tools, which belong to the
  `Internal` group: case creation and report persistence, and [the spawn tool](#5-the-spawn-tool)
  that delegates a task to a fresh utility agent.
- **The preset owns the toolset.** Agents are predefined in `AgentPresetDefinition`, whose
  `ToolGroups` property is an array of group names that decides the agent's tools. The factory
  resolves those names to prefixes and the prefixes to live tools. A call site may **override**
  the preset, but it does not have to.
- **The tool name is the contract.** A tool belongs to a group because its name carries that
  group's prefix. There is no second place where the assignment is written down.
- **Tooling, models and presets are separable.** Tooling, model definitions and agent preset
  definitions are complementary and independently changeable: model definitions are assigned to
  tiers, tiers are assigned to presets, and tools are assigned to agents by their preset
  definition. Tool names can change by the MCP server without notice, but the preset definition
  always resolves the correct tools for the agent from the group names it declares.


---

## Architecture Position

```
┌──────────────────────────────────────────────────────────────────────┐
│                      SentinelCore.Orchestrations                     │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  AgentPresetDefinition                                         │  │
│  │    ToolGroups: ["Forensics", "Persistence", "Execution", ...]  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │ preset default                        │
│                              ▼                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  AgentToolGroupCatalog   (single registry, 15 groups)          │  │
│  │    Forensics   → sc_forensics_                                 │  │
│  │    Persistence → sc_persistence_                               │  │
│  │    Internal    → sc_internal_       ...                        │  │
│  │    Knowledge   → sc_knowledge_                                 │  │
│  │    Find / FindRequired / Claimed / Uncategorized / Suspect     │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │ prefix → tools                        │
│                              ▼                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │  SentinelAgentFactory.ResolveTools(preset, selection?)         │  │
│  │    selection == null  → preset.ToolGroups                      │  │
│  │    selection != null  → override (replaces, never unions)      │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
							  ▲
							  │ IMpcServerRegistry.GetToolsFromCache()
┌──────────────────────────────────────────────────────────────────────┐
│  McpServerRegistry  — StartAsync completes discovery before returning │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 1. The name convention

```
sc_<groupname>_<toolaction>
```

| Element | Rule |
| --- | --- |
| `sc_<groupname>_` | Group prefix, e.g. `sc_networkaccess_`. `sc` is prepended to ensure uniqueness and may also indicate tool vendor in future releases. The prefix indicates the group name and is used to claim tools for that group. |
| `<toolaction>` or `<tool_action>` | lowercase description of the tool action; shape is not important — it is not used in any logic decisions, it is a naming token only. |
| trailing `_` | Required divider. Without it, `sc_forensics_` would claim `sc_forensicsx_...`. |

Comparison is `OrdinalIgnoreCase`. Casing in a tool name is never significant.

The prefix is **derived, never declared**: `AgentToolGroup` takes a 2-parameter constructor
`(name, description)` and computes

```
Prefix = "sc_" + Name.ToLowerInvariant() + "_"
```

The constructor validates the name is non-empty, non-whitespace, and a single segment (it must
not contain `_`), so a group can never emit a prefix that collides with, or nests inside, another
group's prefix. `sc_` alone is never a prefix, because no group is named `""`.

Any tool name that does not start with a registered prefix is **uncategorized**; any tool name
that starts with `sc_` but matches **no** registered prefix is **suspect** (see section 7).

This naming convention allows tools to be fluid and not hardcoded to any specific group or agent.
The MCP server can change the tool names and the Orchestrations will still be able to resolve
them to the correct group based on the prefix. The Sentinel Core solution controls the group
names and prefixes; the MCP Server project owns tool names and conforms to the prefixes defined
in the solution. The MCP Server also provides a manifest tool that lists the tools available in
that release of the server — the Orchestrations invokes it once per process at first startup and
logs its output (section 8).

---

## 2. Groups

`AgentToolGroup` is the value type: `Name`, `Description`, and `Prefix` (derived — the full
expected prefix including the trailing `_`), plus `Claims(string?)` which tests a tool name
against the prefix. Claims is automatic and is not set by the group definition: the prefix alone
decides membership. The catalogue also exposes `Claimed(IEnumerable<string>)` over a live tool
name set, which is convenient for logging and warning generation.

`AgentToolGroupCatalog` is the only registry. Fifteen groups:

| Group | Prefix | Purpose |
| --- | --- | --- |
| `CaseWork` | `sc_casework_` | Case lifecycle, evidence and report persistence |
| `Execution` | `sc_execution_` | Process and command execution |
| `Persistence` | `sc_persistence_` | Scheduled tasks, services, drivers |
| `Forensics` | `sc_forensics_` | Artefact and timeline examination |
| `MemoryInspection` | `sc_memoryinspection_` | Process memory and module inspection |
| `Identity` | `sc_identity_` | Accounts, groups, tokens, privileges |
| `NetworkAccess` | `sc_networkaccess_` | Connections, listeners, shares, remote access |
| `Cryptography` | `sc_cryptography_` | Certificates, keys, hashing |
| `Hardening` | `sc_hardening_` | Policy, audit, firewall, Defender |
| `Configuration` | `sc_configuration_` | Registry, files, environment, installed software |
| `Hardware` | `sc_hardware_` | Devices, drivers, firmware, storage |
| `Utilities` | `sc_utilities_` | Shell, scripting, formatting helpers |
| `WebResearch` | `sc_webresearch_` | External research and documentation lookup |
| `Knowledge` | `sc_knowledge_` | Microsoft Learn documentation search and retrieval (ADR-007) |
| `Internal` | `sc_internal_` | In-process `AIFunction` tools hosted by Orchestrations |

The catalogue's static constructor throws if two groups share a name. That is the only guard it
needs: because prefixes are derived from single-segment names, duplicate prefixes and prefix
nesting (`sc_a_` claiming `sc_a_b_...`) are structurally impossible, and with both impossible
there is no evaluation-order dependence for a tool name to be claimed by two groups.

Agent definitions take an array of group names in the `ToolGroups` property; the groups are
resolved against the catalogue during agent creation (section 3).

---

## 3. Resolution and precedence

```
preset.ToolGroups  →  group names  →  prefixes  →  matching tools     (default)
call-site AgentToolSelection                                          (override)
```

- **No selection passed.** `ResolveTools(preset, null)` resolves `preset.ToolGroups` through the
  catalogue. This is the normal path. A preset is reused by asking for the preset, not by
  re-declaring its tools. A preset with an empty `ToolGroups` list binds nothing, silently — that
  is how tool-free roles such as the Manager are expressed.
- **Selection passed.** It **replaces** the preset default. It does not union with it. Union
  would make it impossible for a call site to *reduce* an agent's toolset, and the spawn tool
  relies on exactly this behaviour: a spawned utility agent receives exactly the groups its
  caller requested, never the preset default plus more.

Tool resolution is driven only by group names and explicit tool names. Roles and the term *domains*
are not part of tool resolution. Roles remain valid for role-specific agent parameterization: each
role can have a different model definition assigned to it.

All group names are resolved through the catalogue, so an unregistered group name fails (see the
failure contract in section 10). Tools that do not begin with a registered prefix are never
included in any grouping; only explicit name selection can bind them, and suspect tools cannot
be bound at all.

---

## 4. Selection kinds

| Kind | Constructor | Meaning |
| --- | --- | --- |
| `None` | `AgentToolSelection.None` | No tools. |
| `All` | `AgentToolSelection.All` | Every tool claimed by a registered prefix. Uncategorized and suspect tools are excluded. |
| `Named` | `FromNames(...)` | Explicit tool names, resolved case-insensitively. Development only. |
| `Groups` | `FromGroups(...)` | Group names, resolved through their prefixes. |

`All` is a **debug catch-all** for development only. It is not a supported production
configuration, and it binds only prefix-claimed tools.

### Usage

```csharp
// The preset default - the preset's ToolGroups decide the tool set.
await _factory.CreateAgentAsync(AgentPreset.WorkerAgent);

// An override that replaces the preset's groups with a single group.
await _factory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromGroups("Forensics"));
```

---

## 5. The spawn tool

`sc_internal_spawn_agent` (Internal group, registered by `SentinelCoreServiceExtensions` as an
`AIFunction` over `AgentSpawnTools.SpawnAgent`):

- Spawns a transient agent from the `UtilityAgent` preset with a generated unique name and a
  curation instruction set, using `AgentToolSelection.FromGroups(...)` supplied by the caller —
  so the spawned agent receives exactly the requested groups (a selection replaces the preset
  groups, never unions with them).
- Runs exactly one task with `RunAsync` and returns the agent's answer inside a `ToolResult`.
- Does not retain the agent: it lives for the task and is destroyed afterwards. No session is
  persisted and the agent is never registered anywhere.
- Resolves the agent factory lazily from the service provider at invocation time, so there is no
  construction cycle between the factory and the tool it hosts.

The purpose is to let The Core answer small environmental questions ("What is the current
firewall status on this machine?") by dispatching a fresh utility agent with the right group of
tools, without creating a case or starting an investigation workflow. The Core defines the task
and receives the results from the spawned agent. This simplifies the main workflow decision tree.

---

## 6. Preset assignments

Every assignment is the preset default. Any preset can be overridden by the tool selection
parameter of `CreateAgentAsync` (section 3), which replaces the preset's `ToolGroups` with the
specified groups or tool names; spawned utility agents do exactly that. `UtilityAgent` carries
only `Utilities`, so platform-interrogation groups reach it only through an explicit call-site
selection.

| Preset | `ToolGroups` | Rationale |
| --- | --- | --- |
| `CoreAgent` | `Internal`, `Knowledge`, `Utilities` | Human interface. The Core answers case questions and general chat, searches Microsoft Learn documentation, and dispatches spawned agents for anything deeper, so it must not carry operating-system tools or their results. RAG search is provided through middleware, not a tool group. |
| `ManagerAgent` | *(none)* | PL-3 forbids tools on the Manager. Enforced both by the preset and by a factory guard. |
| `WorkerAgent` | `Forensics`, `Persistence`, `Execution`, `MemoryInspection`, `Identity`, `NetworkAccess`, `Cryptography`, `Utilities` | The investigating agent. |
| `SafetyAgent` | `Hardening`, `Identity`, `Configuration`, `Utilities` | Policy, posture and account review. |
| `AggregatorAgent` | `Internal`, `Utilities` | Needs the internal tools for evidence verification while generating the report. |
| `ClassifierAgent` | `Utilities` | Returns structured JSON; it has nothing platform-specific to call. |
| `UtilityAgent` | `Utilities` | Shell and formatting helpers. Spawned agents override this selection. |
| `ReportExtractorAgent` | `Internal`, `Utilities` | Internal and formatting tools. |

---

## 7. Uncategorized and suspect tools

Any tool whose name matches no registered prefix is classified by whether it carries the `sc_`
root:

**Uncategorized** — the name is foreign to the naming convention entirely:

- It is excluded from `All`.
- It is excluded from every group.
- It remains reachable by explicit name through `FromNames(...)`, so the call site decides
  whether to use it.
- It is logged as a warning on every tool-cache refresh, and `IModelConfigGate` reports it.

**Suspect** — the name starts with `sc_` (OrdinalIgnoreCase) but no registered group claims it:

- It is logged as an **error** with as much detail as possible: the tool name, the `sc_` root,
  and the registered group names. The condition may be simply a typo, but it could also indicate
  a malicious tool being added to the MCP server.
- It is excluded from every group, from `All`, **and from name resolution**: a suspect tool is
  unreachable by design. The factory refuses to bind it even when explicitly requested by name.
- `IModelConfigGate` reports it.

This is a security measure to prevent any potential malicious tools from being used in the
investigation process. It is not a hard security measure — it highlights the condition rather
than blocking startup.

---

## 8. Logging and the manifest tool

- The factory logs the tool count and names on every tool-cache refresh, including the snapshot
  taken at construction, so tools discovered before the factory existed are still reported.
- Any discrepancy discovered in counts, names, or prefixes is logged as a warning or error per
  section 7.
- The MCP server publishes a **manifest tool** (its name contains `manifest`). The
  `McpServerRegistry` invokes it once per process, after the first successful server start, and
  logs the output at Information level. A failure to find or invoke the manifest tool is logged
  as a warning and never blocks the server start. Manifest output at first startup is the
  authoritative record of what tools a server release exposed.

---

## 9. Knowledge tools

The Knowledge group is populated by three in-process tools over the Microsoft Learn MCP endpoint
(`sc_knowledge_search_docs`, `sc_knowledge_fetch_docs`, `sc_knowledge_search_code_samples`),
hosted by `MsLearnMcpTools`. They are a **temporary implementation with a mandatory re-design in
the future**; they are brittle and may break without notice. They must not be shipped in the
SentinelCore-MCP server. [ADR-007](ADR/007-MsLearn-Knowledge-Tools-Temporary.md) is the
governing decision record.

---

## 10. Failure contract

| Condition | Behaviour |
| --- | --- |
| A preset or call site names a group that is not registered | **Throw** `InvalidOperationException` naming the group and listing the valid names |
| A call site names a tool that is not in the catalogue | **Log warning** (with the available tool names), skip the name, continue |
| A call site names a suspect tool | **Log error** with maximum detail, refuse to bind, continue |
| A call site names an uncategorized tool | **Bind it** — explicit name selection is the only way to reach one |
| A registered group claims zero live tools | **Log warning**, bind nothing for that selection, continue |
| A tool matches no registered prefix | Uncategorized or suspect; logged, excluded from every grouping, suspect additionally unreachable by name |
| A tool selection kind is unsupported | **Throw** `InvalidOperationException` |

An unknown **group** is a configuration defect in code, so it fails fast. An unknown **tool
name** is a statement about a server that changes without notice, so it must never block. An
empty **group** is normal while a server has not adopted the convention, so it is a soft
warning.

---

## 11. Ordering requirement

Prefix matching requires the tool catalogue to be populated **before** any agent is created. This is
already satisfied by the existing initialization chain and required no new code:

1. `McpServerRegistry.StartAsync` runs `ConnectAsync` → `ListToolsAsync` → publish `ToolNames` →
   `RefreshToolCacheAsync`, and does not return until that sequence completes.
2. `AISystemsInitialization.ExecuteAsync` awaits MCP server start before `InitializeOrchestrationAsync`.
3. `InitializeOrchestrationAsync` → `OrchestrationControl.InitializeAsync` → `CreateAgentAsync`.

The factory additionally subscribes to `ToolCacheChanged` and refreshes its cache, so a server that
finishes late does not leave stale tools bound.

---

## 12. Readiness gate

`IModelConfigGate` has blocking-readiness and warning-generation surfaces. The gate is global because
`Send()` starts the entire master workflow, not just the Core agent.

| Tier | Trigger | Current effect |
| --- | --- | --- |
| **Blocking** | Ollama unreachable, or any registered MCP server is not `Connected` | The gate reports failure and the UI blocks the send action |
| **Warning generation** | A group resolves to zero tools, `Uncategorized` is non-empty, `Suspect` is non-empty, or a named tool failed to resolve | The factory logs it (suspect tools as errors) and the gate can surface it as banner text; `CoreChatViewModel` does not yet consume `GetReadinessWarnings()` |
| **Silent** | `None`, or a narrow selection that resolved successfully | Nothing |

The servers that ship in this release are internal and there is no consumer-facing server-add UI, so
a server that is down produces a visible warning and a degraded agent — not a terminal failure.
Tool warnings are logged and reported through the gate; the chat UI does not surface them yet
(see [Open Items / TODOs](#open-items--todos)).

---

## 13. Drift rules

When adding or changing a tool:

1. Name it `sc_<groupprefix>_<verb>_<noun>` using the prefix of the group that should own it.
2. If no group fits, add a group to `AgentToolGroupCatalog` — the prefix is derived from the
   name — and update the presets that should receive it. Do **not** add a rule, a name list, or
   a second prefix.
3. Watch for the uncategorized warning in the log. A warning is the signal that step 1 was
   skipped. It is not an error. A suspect **error** is the signal that a `sc_`-named tool is
   claimed by no group — fix the name or the group registry.

When adding an MCP server: register it and confirm its tool names use the convention. Tools whose
names deviate will appear as uncategorized rather than being silently unreachable; `sc_`-named
tools that deviate will appear as suspect errors.

Do not reintroduce name lists or match rules. A list that names tools loses them when a name changes;
that is the defect the prefix scheme (ADR-006) removes.

---

## Pattern-Lock Compliance

- **PL-3 (Agent Construction).** The Manager agent must not have tools. The invariant is enforced
  twice: the `ManagerAgent` preset declares an empty `ToolGroups` list, and the factory guard
  (`EnsureManagerHasNoTools`) throws when a Manager construction resolves a non-empty toolset.
- Agents are constructed only through `ISentinelAgentFactory`. Function invocation is handled by
  `ChatClientAgent` automatically, so no `UseFunctionInvocation` decorator is added to the builder
  pipeline.

---

## Open Items / TODOs

- The Knowledge tools (section 9) are a temporary implementation with a mandatory re-design
  (ADR-007). They must not ship in the SentinelCore-MCP server.
- `CoreChatViewModel` does not consume `IModelConfigGate.GetReadinessWarnings()` yet; tool-warning
  surfacing in the chat UI is not implemented and no readiness banner is displayed (section 12).
- The MCP server feature ships without a consumer-facing server-add UI in this release; the shipped
  servers are internal.

---

## Related Documentation

- [ADR-006](ADR/006-Tool-Group-Prefix-Scheme.md) — the tool-group prefix scheme.
- [ADR-007](ADR/007-MsLearn-Knowledge-Tools-Temporary.md) — the temporary Microsoft Learn knowledge tools.
- [ADR-005](ADR/005-Tool-Domain-Vocabulary.md) — the superseded domain vocabulary, retained for history.
- [McpServerRegistryComponent.md](McpServerRegistryComponent.md) — server discovery, start sequence and tool-cache publication.

---

## Version History

| Version | Date | Author | Changes |
| --------- | ------ | -------- | --------- |
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
| v2.0 | 2026-07-19 | Kyle | Replaced the removed `ToolRegistry` shape with the rule-based domain vocabulary |
| 2.1.0 | unknown | unknown | Clarified that domain warning data is generated/logged but `CoreChatViewModel` does not currently consume `GetReadinessWarnings()` |
| v3.0 | 2026-07-19 | Kyle | Replaced the rule-based domain vocabulary with the prefix-based tool group scheme per ADR-006: `AgentToolGroup` / `AgentToolGroupCatalog`, preset-owned `ToolGroups`, optional override, `Uncategorized` safety net, no role selector |
| v3.1 | 2026-11-18 | Kyle | Removed the superseded-shape snippets that reproduced deleted code, and replaced the deleted domain docs with this document as the single tooling reference. |
| v4.0 | 2026-11-18 | Kyle | Rewritten after the tooling refactor: derived prefixes with single-segment validation, 15th `Knowledge` group (ADR-007), suspect-tool classification and unreachability, non-throwing name resolution, spawn tool, manifest logging, updated preset matrix, All selection restricted to claimed tools. |
| v4.1 | 2026-11-19 | Copilot | Reorganized into the standard component format: removed the review commentary and folded it into Purpose, resolution and preset assignments, numbered the spawn-tool section, added Pattern-Lock Compliance, Open Items / TODOs and Related Documentation, and corrected stale section cross-references and the ADR-007 link. |
