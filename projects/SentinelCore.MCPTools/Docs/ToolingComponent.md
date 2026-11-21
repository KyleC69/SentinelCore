---
title: "Tooling Component"
status: Active
component: Tooling
last_updated: 2026-11-18
version: v3.1
---

# SentinelCore Tooling Component

**Project:** `SentinelCore.Orchestrations`
**Namespace:** `SentinelCore.Orchestrations.Abstractions`
**Dependencies:** `SentinelCore.Contracts` (MCP contracts, `AITool`), `Microsoft.Extensions.AI`
**Consumers:** `SentinelAgentFactory` (tool resolution), `TheCoreWorkflow`, `ConcurrentGroupTeam`, `CustomGroupWorkflow`
**Governing decision:** [ADR-006](ADR/006-Tool-Group-Prefix-Scheme.md)

---

## Purpose

The MCP server (SentinelCore-MCP) supplies the read-only operating-system interrogation surface.
The Orchestrations project supplies a small set of in-process `AIFunction` tools for mutation.

**Key Principle:** Tools are **read-only** — they gather evidence, they don't mutate system state.

Mutation is handled by the internal local tools, which belong to the `Internal` group.

**Second Key Principle:** An agent's toolset is **owned by its preset**. The preset declares group
names; the factory resolves those names to prefixes and the prefixes to live tools. A call site may
**override** the preset, but it does not have to.

**Third Key Principle:** The **tool name is the contract**. A tool belongs to a group because its name
carries that group's prefix. There is no second place where the assignment is written down.

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
│  │  AgentToolGroupCatalog   (single registry, 14 groups)          │  │
│  │    Forensics   → sc_forensics_                                 │  │
│  │    Persistence → sc_persistence_                               │  │
│  │    Internal    → sc_internal_       ...                        │  │
│  │    Find / FindRequired / Claimed / Uncategorized               │  │
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
sc_<groupprefix>_<verb>_<noun>
```

| Element | Rule |
| --- | --- |
| `sc_` | Platform root. `AgentToolGroup.NamePrefixRoot`. Every group prefix must begin with it. |
| `<groupprefix>` | One lowercase segment, unique across the catalogue. |
| trailing `_` | Required. Without it, `sc_forensics_` would claim `sc_forensicsx_...`. |

Comparison is `OrdinalIgnoreCase`. Casing in a tool name is never significant.

A prefix that lacks the root, lacks the trailing underscore, or names no segment is rejected by the
`AgentToolGroup` constructor. A bare `sc_` is rejected specifically, because it would claim every
prefixed tool and collapse the partition.

---

## 2. Groups

`AgentToolGroup` is the value type: `Name`, `Description`, `Prefix`, and `Claims(string?)`.

`AgentToolGroupCatalog` is the only registry. Fourteen groups:

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
| `Internal` | `sc_internal_` | In-process `AIFunction` tools hosted by Orchestrations |

The catalogue's static constructor throws if two groups share a name, share a prefix, or if one
prefix **contains** another. Nesting matters: with both `sc_a_` and `sc_a_b_` registered, the tool
`sc_a_b_do_thing` would be claimed by two groups and membership would depend on evaluation order
rather than on the name alone. The guard turns that into a startup failure.

---

## 3. Resolution and precedence

```
preset.ToolGroups  →  group names  →  prefixes  →  matching tools     (default)
call-site AgentToolSelection                                          (override)
```

- **No selection passed.** `ResolveTools(preset, null)` resolves `preset.ToolGroups`. This is the
  normal path. A preset is reused by asking for the preset, not by re-declaring its tools.
- **Selection passed.** It **replaces** the preset default. It does not union with it. Union would make
  it impossible for a call site to *reduce* an agent's toolset.

All group names are resolved to prefixes **before** any tool is bound, so an unregistered group fails
even when another group in the same list would have produced tools.

---

## 4. Selection kinds

| Kind | Constructor | Meaning |
| --- | --- | --- |
| `None` | `AgentToolSelection.None` | No tools. |
| `All` | `AgentToolSelection.All` | Every tool claimed by a registered prefix. Uncategorized tools are excluded. |
| `Named` | `FromNames(...)` | Explicit tool names, resolved case-insensitively. |
| `Groups` | `FromGroups(...)` | Group names, resolved through their prefixes. |

`FromRole` and `FromDomains` do not exist. Role-based assignment is expressed by choosing the preset.
There is deliberately no role selector on the override parameter: a call site that wants role tools
names the preset.

`All` is a **debug catch-all**. It is not a supported production configuration.

### Usage

```csharp
// The preset default - the preset's ToolGroups decide the tool set.
await _factory.CreateAgentAsync(AgentPreset.WorkerAgent);

// An override that replaces the preset's groups with a single group.
await _factory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromGroups("Forensics"));
```

---

## 5. Preset assignments

| Preset | `ToolGroups` | Rationale |
| --- | --- | --- |
| `CoreAgent` | `Internal`, `WebResearch` | Human interface. The Core answers case questions and general chat and dispatches workers for anything deeper, so it must not carry operating-system tools or their results. RAG search will be added here later. |
| `ManagerAgent` | *(none)* | PL-3 forbids tools on the Manager. Enforced both by the preset and by a factory guard. |
| `WorkerAgent` | `Forensics`, `Persistence`, `Execution`, `MemoryInspection`, `Identity`, `NetworkAccess`, `Cryptography` | The investigating agent. |
| `SafetyAgent` | `Hardening`, `Identity`, `Configuration` | Policy, posture and account review. |
| `AggregatorAgent` | `Internal` | Needs the documentation tools for evidence verification while generating the report. |
| `ClassifierAgent` | *(none)* | Returns structured JSON; it has nothing to call. |
| `UtilityAgent` | `Utilities` | Shell and formatting helpers. |
| `ReportExtractorAgent` | `Internal` | Documentation tools only. |

---

## 6. Uncategorized

Any tool whose name matches no registered prefix is **uncategorized**:

- It is excluded from `All`.
- It is excluded from every group.
- It remains reachable by explicit name through `FromNames(...)`.
- The factory logs the count on every tool-cache refresh.
- `IModelConfigGate` reports it as a warning.

**Current state (2026-11-18):** the MCP server's 141 tools now carry `sc_*` prefixes and
resolve through the groups above. The tool-level grouping is visible to agents via the
server's `Server_Read_Manifest` capability (`sc_utilities_read_server_manifest`), which
reflects the registered `[McpServerTool]` attributes. `Casework` and `WebResearch` have
no server tools (they are mutation/documentation groups hosted by Orchestrations).

---

## 7. Failure contract

| Condition | Behaviour |
| --- | --- |
| A preset or call site names a group that is not registered | **Throw** `InvalidOperationException` naming the group and listing the valid names |
| A call site names a tool that is not in the catalogue | **Log warning**, bind the names that did resolve, continue |
| A registered group claims zero live tools | **Log warning**, bind nothing, continue |
| A tool matches no registered prefix | Uncategorized; logged, excluded from `All`, reachable by name |

An unknown **group** is a configuration defect in code, so it fails fast. An unknown **tool name** is a
statement about a server that changes without notice, so it must never block. An empty **group** is
normal while the server has not adopted the convention, so it is a soft warning.

---

## 8. Ordering requirement

Prefix matching requires the tool catalogue to be populated **before** any agent is created. This is
already satisfied by the existing initialization chain and required no new code:

1. `McpServerRegistry.StartAsync` runs `ConnectAsync` → `ListToolsAsync` → publish `ToolNames` →
   `RefreshToolCacheAsync`, and does not return until that sequence completes.
2. `AISystemsInitialization.ExecuteAsync` awaits MCP server start before `InitializeOrchestrationAsync`.
3. `InitializeOrchestrationAsync` → `OrchestrationControl.InitializeAsync` → `CreateAgentAsync`.

The factory additionally subscribes to `ToolCacheChanged` and refreshes its cache, so a server that
finishes late does not leave stale tools bound.

---

## 9. Readiness gate

`IModelConfigGate` has blocking-readiness and warning-generation surfaces. The gate is global because
`Send()` starts the entire master workflow, not just the Core agent.

| Tier | Trigger | Current effect |
| --- | --- | --- |
| **Blocking** | Ollama unreachable, or any registered MCP server is not `Connected` | `CanSend()` reports failure and the UI blocks the send action |
| **Warning generation** | A group resolves to zero tools, or `Uncategorized` is non-empty, or a named tool failed to resolve | The factory logs it and the gate can surface it as banner text; `CoreChatViewModel` does not yet consume `GetReadinessWarnings()` |
| **Silent** | `None`, or a narrow selection that resolved successfully | Nothing |

The MCP server feature does not ship with a consumer-facing server-add UI in this release. The servers
that ship are internal, so a server that is down produces a visible warning and a degraded agent — not
a terminal failure. Tool warning surfacing in the chat surface remains incomplete and should not be
described as a displayed banner.

---

## 10. Drift rules

When adding or changing a tool:

1. Name it `sc_<groupprefix>_<verb>_<noun>` using the prefix of the group that should own it.
2. If no group fits, add a group to `AgentToolGroupCatalog` — one prefix, one name — and update the
   presets that should receive it. Do **not** add a rule, a name list, or a second prefix.
3. Watch for the uncategorized warning in the log. A warning is the signal that step 1 was skipped. It
   is not an error.

When adding an MCP server: register it and confirm its tool names use the convention. Tools whose
names deviate will appear as uncategorized rather than being silently unreachable.

Do not reintroduce name lists or match rules. A list that names tools loses them when a name changes;
that is the defect ADR-005 failed to remove and ADR-006 removes.

---

## Version History

| Version | Date | Author | Changes |
| --------- | ------ | -------- | --------- |
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
| v2.0 | 2026-07-19 | Kyle | Replaced the removed `ToolRegistry` shape with the rule-based domain vocabulary |
| 2.1.0 | unknown | unknown | Clarified that domain warning data is generated/logged but `CoreChatViewModel` does not currently consume `GetReadinessWarnings()` |
| v3.0 | 2026-07-19 | Kyle | Replaced the rule-based domain vocabulary with the prefix-based tool group scheme per ADR-006: `AgentToolGroup` / `AgentToolGroupCatalog`, preset-owned `ToolGroups`, optional override, `Uncategorized` safety net, no role selector |
| v3.1 | 2026-11-18 | Kyle | Removed the superseded-shape snippets that reproduced deleted code, and replaced the deleted domain docs with this document as the single tooling reference. |
