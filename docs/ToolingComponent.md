---
title: "Tooling Component"
status: Active
component: Tooling
last_updated: 2026-07-19
version: v2.0
---

# SentinelCore Tooling Component

**Project:** `SentinelCore.Orchestrations`
**Namespace:** `SentinelCore.Orchestrations.Abstractions`
**Dependencies:** `SentinelCore.Contracts` (MCP contracts, `AITool`), `Microsoft.Extensions.AI`
**Consumers:** `SentinelAgentFactory` (tool resolution), `TheCoreWorkflow`, `ConcurrentGroupTeam`, `CustomGroupWorkflow`

---

## Purpose

The MCP server (SentinelCore-MCP) supplies the read-only operating-system interrogation surface and defines 140+ tools.
The Orchestrations project supplies a small set of in-process `AIFunction` tools for mutation.

**Key Principle:** Tools are **read-only** — they gather evidence, they don't mutate system state.

Mutation is handled by the internal local tools, which are named with the `local_` prefix.

**Second Key Principle:** An agent's toolset is a **call-site decision**. Nothing in the preset or the MCP server
definition decides which tools an agent receives.

---

## Architecture Position — superseded diagram

> The diagram below shows the removed `ToolRegistry` shape. The current composition is described in sections 1-5:
> the call site supplies an `AgentToolSelection` and `SentinelAgentFactory.ResolveTools` turns it into a toolset.

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  Tooling Component                         │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │           ToolRegistry (static class)               │  │  │
│  │  │  • DomainToolNames: Dictionary<string, string[]>   │  │  │
│  │  │  • RoleToolNames: Dictionary<AgentRole, string[]>  │  │  │
│  │  │  • GetToolByDomain(domain) → IList<AITool>?        │  │  │
│  │  │  • GetToolsByNames(names) → IReadOnlyList<AITool>  │  │  │
│  │  │  • GetToolsetByRole(role) → IReadOnlyList<AITool>  │  │  │
│  │  │  • GetAllTools() → IReadOnlyList<AITool>           │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ consumed by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  AgentSpecBuilder.GetToolsetByRole(role)                        │
│  DomainAgentFactory.CreateAgent(domain, description)            │
│  CoreAgentFactory.Create()  (adds MCP tools on top)             │
│  MagneticCoopOrchestration (direct role-based toolsets)         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Tool domains — rules, never lists

The earlier design mapped a domain to a fixed array of tool names. That design has one fatal flaw: **when a tool is
added or renamed, every domain that named it silently loses it.** A domain that lists `read_registry_key` stops
working the moment the server renames it to `Registry_Read_Key`, and nothing reports the loss.

The delivered design maps a domain to **membership rules over names** instead. Membership is recomputed against the
live catalogue on every resolution, so:

- a tool added later is claimed automatically by its area prefix;
- a tool renamed into a different area is claimed by its new area;
- a tool that no rule claims is **reported** rather than silently dropped — see `AgentToolDomainCatalog.Unclassified`.

Domains may overlap on purpose. `Registry_Read_Acl` belongs to both `Registry` and `Permissions`; the factory
de-duplicates by tool name so the model call never sees the same tool twice.

**File:** `Abstractions/AgentToolSelection.cs`
**Types:** `AgentToolDomain`, `ToolMatchRule`, `AgentToolDomainCatalog`

| Domain | Rules | Purpose |
| ---------- | --------- | --------- |
| `AllowedCommands` | `AllowedCommands_` | Configured command allow-list surface |
| `Auditing` | `Auditing_`, tokens `AuditPol`, `AccessChk` | Audit policy, effective permissions |
| `Autoruns` | `Autoruns_` | Autorun locations, image-hijack persistence |
| `Bitlocker` | `Bitlocker_` | BitLocker status, protectors, recovery keys |
| `Browser` | `Browser_` | Browser policy, profiles, extensions |
| `CaseLifecycle` | `local_case_` | Internal case creation and report persistence |
| `Certificates` | `Certificate_`, tokens `Certificates`, `SigCheck` | Code-signing certificates, signatures |
| `Com` | `COM_` | COM registration, component hijacking |
| `Defender` | `Defender_` | Defender status, scan and threat history |
| `Devices` | `Device_`, tokens `Devices`, `PnP`, `PnpListDevices` | Plug-and-play devices |
| `Documentation` | `local_docs_`, tokens `mslearn`, `docs` | Vendor documentation lookups |
| `Driver` | `Driver_`, token `Drivers` | Kernel and file-system drivers |
| `Dns` | `DNS_` | Resolution and resolver cache |
| `EventLog` | `Event_Log_`, token `EventLog` | Event channels and queries |
| `FileSystem` | `File_System_`, token `FileSystem` | Files, streams, hashes, ACLs, metadata |
| `Firewall` | `Firewall_` | Firewall profiles and rules |
| `GroupPolicy` | `Group_Policy_`, tokens `GroupPolicy`, `GPO` | GPO and resultant policy |
| `Hotfix` | `Hotfix_`, token `Hotfixes` | Installed updates |
| `Logon` | `Logon_` | Interactive and network logons |
| `Memory` | `Memory_`, tokens `VMMap`, `RAMMap` | Physical and virtual memory |
| `Modules` | `Module_`, tokens `Modules`, `ListDlls` | Loaded modules and DLLs |
| `Network` | `Network_` | Connections, ARP cache, routes, adapters |
| `Permissions` | tokens `Acl`, `AccessChk`, `Access` | ACLs and effective permissions |
| `Pipes` | `Pipe_`, tokens `Pipes`, `PipeList` | Named pipes |
| `PowerShell` | `PowerShell_` | Read-only PowerShell execution and history |
| `Process` | `Process_`, tokens `PsList`, `ProcDump`, `Handle` | Processes, handles, dumps |
| `Registry` | `Registry_` | Registry keys, values, hives |
| `ScheduledTasks` | `ScheduledTasks_`, token `ScheduledTask` | Tasks and triggers |
| `SearchIndex` | `Search_`, token `SearchIndex` | Windows Search crawl scope |
| `Security` | `Security_` | Security policy, zones, hardening |
| `Service` | `Service_`, token `Services` | Services, state and configuration |
| `Sessions` | `Sessions_`, token `LogonSessions` | Logon and remote sessions |
| `ShellTaskbar` | `Shell_Taskbar_`, token `ShellTaskbar` | Shell and taskbar configuration |
| `Software` | `Software_` | Installed software inventory |
| `System` | `System_` | System information, machine state |
| `Tokens` | `Token_`, token `Tokens` | Token privileges and integrity level |
| `Uac` | `UAC_` | User Account Control, elevation state |
| `Volume` | `Volume_`, token `Volumes` | Volumes, partitions, disk layout |
| `WebResearch` | `Web_`, tokens `WebSearch`, `WebResearch` | Web search and page retrieval |
| `Wmi` | `WMI_` | WMI namespaces, classes and queries |

> Every single-segment stem declared through the `Area(...)` helper contributes **both** a prefix rule and a token
> rule. The token rule is what keeps a vendor-prefixed name such as `Sysinternals_PsList_List_Processes` claimable
> without naming the vendor, and it is why a singular/plural change alone cannot empty a domain.

---

## 2. Matchers

**Type:** `ToolMatchRule` — only two strategies exist, and neither can throw during matching.

| Matcher | Semantics | Example |
| --------- | ----------- | --------- |
| `Prefix(stem)` | The name starts with the stem plus `_`. The separator is appended automatically. | `Prefix("Power")` becomes `Power_`, which does **not** claim `PowerShell_Run_Command` |
| `Token(token)` | Any underscore-delimited segment of the name equals the token, case-insensitively. Vendor independent. | `Token("PsList")` claims both `PsList_List_Processes` and `Sysinternals_PsList_List_Processes` |

There is deliberately no `Suffix`, no `Exact`, and no throwing matcher. A rule that cannot match simply claims
nothing, and `Unclassified` surfaces the consequence.

---

## 3. Categories — composite domains

**Property:** `AgentToolDomainCatalog.Categories`

A category is itself an `AgentToolDomain` whose rules are the union of its members' rules. It therefore flows through
the same resolution path and needs no separate selection kind.

| Category | Member domains |
| ---------- | ---------------- |
| `Forensics` | FileSystem, Registry, EventLog, Process, Service, Driver, ScheduledTasks |
| `Persistence` | Autoruns, ScheduledTasks, Service, Driver, Com, ShellTaskbar |
| `Identity` | Security, Logon, Sessions, Tokens, Uac |
| `NetworkAccess` | Network, Firewall, Dns |
| `Cryptography` | Certificates, Bitlocker |
| `Configuration` | GroupPolicy, Auditing, Wmi, System, AllowedCommands |
| `Execution` | Process, PowerShell, Com, Wmi |
| `MemoryInspection` | Memory, Modules, Pipes |
| `Hardware` | Devices, Driver, Volume, Hotfix, Software |
| `Hardening` | Defender, Security, Auditing, Firewall, Permissions |
| `CaseWork` | CaseLifecycle, Documentation |
| `Utilities` | System, SearchIndex, Browser, Volume |

---

## 4. Role toolsets

**Type:** `AgentToolRoleToolsets` — maps an `AgentPreset` to the domains that role may use, so a call site names the
job rather than a list of tools.

| Preset | Toolset | Domains |
| -------- | --------- | --------- |
| `CoreAgent` | `CoreReasoning` | CaseLifecycle, Documentation, WebResearch |
| `SafetyAgent` | `Guardrails` | Security, Auditing, Permissions, Defender, Firewall, GroupPolicy, Uac |
| `WorkerAgent` | `CaseWork` | FileSystem, Registry, EventLog, Process, Service, Driver, ScheduledTasks |
| `UtilityAgent` | `Utilities` | System, SearchIndex, Browser, Volume |
| `AggregatorAgent` | `EvidenceVerification` | Documentation |
| `ManagerAgent` | none | PL-3 forbids tools on the Manager |
| `ClassifierAgent` | none | The Classifier only routes input |

The Core agent is the human interface. It answers case questions and general knowledge chat, and it dispatches
workers for deeper status checks so its own context stays free of tool results. It therefore holds **no**
operating-system domains. RAG search domains will join `CoreReasoning` when the RAG engine lands.

---

## 5. Selection kinds

| Kind | Factory | Behaviour | Failure mode |
| ------ | --------- | ----------- | -------------- |
| `None` | `AgentToolSelection.None` | Binds nothing | none |
| `All` | `AgentToolSelection.All` | Binds **every** tool in the merged catalogue, MCP and local, with no filters | none |
| `Named` | `FromNames(...)` | Binds exactly the names given; duplicates collapsed | An unknown name throws — a **feature block**, not process death |
| `Domains` | `FromDomains(...)`, `FromRole(...)` | Binds the union of what the domains' rules claim | An empty result logs a warning and binds nothing — **soft**, never fatal |

`All` is the debug catch-all. It must stay unfiltered: any MCP server loaded at startup is automatically included.
Only the testing harness (`CustomGroupWorkflow`) uses it.

`Unclassified` is the structural guarantee behind the whole design:

```csharp
IReadOnlyList<string> unclaimed = AgentToolDomainCatalog.Unclassified(liveToolNames);
```

A non-empty result means a tool exists that no domain claims. The factory logs it on every tool-cache refresh and the
UI surfaces it as a warning. The tool is still reachable through `All` and by explicit name — it is never lost.

---

## 6. Readiness gate

`IModelConfigGate` has three tiers. The gate is global because `Send()` starts the entire master workflow, not just
the Core agent.

| Tier | Trigger | Effect |
| ------ | --------- | -------- |
| **Blocking** | Ollama unreachable, or any registered MCP server not `Connected` | The banner names the missing service; agent actions are blocked |
| **Warning** | A selected domain or role resolves to zero tools; `Unclassified` is non-empty | Banner only; the workflow still runs |
| **Silent** | `None`, or a narrow selection that resolved successfully | Nothing |

MCP loss is therefore **fail-fast and visible, not terminal**: the same banner that reports a missing Ollama instance
reports a disconnected server, and the system recovers as soon as the server reconnects.

---

## 7. Drift rules

When adding or changing a tool:

1. Name it `<Area>_<Verb>_<Noun>` using the area prefix of the domain that should own it.
2. Add a domain rule only when the new area is not covered. Prefer `Area(...)` over hand-written rules.
3. Watch for the `Unclassified` warning in the log. A warning is the signal that step 1 or 2 was skipped — it is
   not an error.

When adding an MCP server: register it, then confirm its tool names use area prefixes. Tools whose names deviate will
appear in `Unclassified` rather than being silently unreachable.

---



## 8. Superseded shape (reference only)

The shape below was removed. It is kept here so the difference stays obvious: a domain that names tools loses them
when a name changes.

```csharp
// REMOVED — do not reintroduce.
private static readonly Dictionary<string, string[]> DomainToolNames = new(StringComparer.OrdinalIgnoreCase)
{
    ["registry"] = ["read_registry_key", "search_registry_keys", "read_registry_value"],
    ["filesystem"] = ["read_file", "list_directory", "search_files", "get_file_hash"],
    // ...one fixed array per area...
};
```

The replacement is a rule set per area, for example:

```csharp
public static AgentToolDomain Registry { get; } =
    new("Registry", "Registry keys, values and hives.", Area("Registry"));
```

The role axis followed the same change. `RoleToolNames` keyed on the deleted `AgentRole` enum and listed tool names;
`AgentToolRoleToolsets` now keys on `AgentPreset` and lists **domains**:

```csharp
// Current.
public static IReadOnlyList<AgentToolDomain> CoreReasoning { get; } =
[
    AgentToolDomainCatalog.CaseLifecycle,
    AgentToolDomainCatalog.Documentation,
    AgentToolDomainCatalog.WebResearch
];
```

Call sites use `AgentToolSelection.FromRole(AgentPreset.CoreAgent)`. A call site that needs something narrower or
wider passes `FromNames(...)` or `FromDomains(...)` instead; the role toolset is a default, not a cage.

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
| v2.0 | 2026-07-19 | Kyle | Replaced the removed `ToolRegistry` shape with the delivered rule-based domain vocabulary: `AgentToolDomain` / `ToolMatchRule` / `AgentToolDomainCatalog`, role toolsets keyed on `AgentPreset`, four selection kinds, `Unclassified` safety net, and the three-tier readiness gate |
