## Architecture Decision Record: Prefix-Based Tool Groups (ADR-006)

**Status:** Accepted

## Amendment (2026-11-18, refactor complete)

The tooling refactor described in this ADR is complete. This amendment records the resulting state of the scheme; the original decision content of this ADR is preserved unchanged below.

1. **A 15th tool group, `Knowledge`, was added**, with the purpose "Microsoft Learn documentation search and retrieval". It currently hosts the temporary Microsoft Learn implementation documented in [ADR-007](007-MsLearn-Knowledge-Tools-Temporary.md).

2. **Group prefixes are derived, not declared.** `AgentToolGroup` now has a two-parameter constructor `(name, description)` and computes `Prefix = "sc_" + Name.ToLowerInvariant() + "_"`, with the constant root `NamePrefixRoot = "sc_"`. The constructor validates that the name is non-empty, non-whitespace, and a single segment (no underscore), which makes duplicate prefixes and prefix nesting structurally impossible. The `AgentToolGroupCatalog` static guard therefore keeps only the duplicate-name check; the old duplicate-prefix and contains-prefix checks were removed.

3. **Suspect tool classification was added.** A tool name that starts with `sc_` (OrdinalIgnoreCase) but that no registered group claims is a SUSPECT tool: it is error-logged with maximum detail (possible typo vs. possible malicious tool added to the MCP server), excluded from every group, excluded from the `All` selection, and unreachable by name resolution. Uncategorized tools (names foreign to the naming convention) remain warnings: excluded from groupings and from `All`, but reachable by explicit name.

4. **The preset tool-group matrix was updated:** CoreAgent = Internal + Knowledge + Utilities; ManagerAgent = none (PL-3 unchanged); SafetyAgent = Hardening + Identity + Configuration + Utilities; UtilityAgent = Utilities; ClassifierAgent = Utilities; WorkerAgent = Forensics + Persistence + Execution + MemoryInspection + Identity + NetworkAccess + Cryptography + Utilities; AggregatorAgent = Internal + Utilities; ReportExtractorAgent = Internal + Utilities.

5. **A new internal tool, `sc_internal_spawn_agent`, was added to the Internal group.** It spawns a transient one-shot agent from the UtilityAgent preset with an explicit `AgentToolSelection.FromGroups(...)` (a selection REPLACES the preset groups, never unions), runs a single task, returns the agent's answer in a ToolResult, and does not retain the agent. It exists so The Core can answer small environmental questions without starting an investigation workflow.

6. **Name resolution (`AgentToolSelection.FromNames`) no longer throws on unknown names:** unknown names log a warning and are skipped; suspect names log an error and are never bound; uncategorized names are bound when explicitly requested.

**Status:** Accepted — supersedes ADR-005
**Date:** 2026-07-19
**Architects:** [Agent/Copilot]
**Impacted Components:** `SentinelCore.Orchestrations` (`Abstractions/`, `Agents/`, `Workflows/`, `Infrastructure/DI/`), `SentinelCore.UI` (`Services/`), `SentinelCore.Tests`

---

### 1. Context and Problem Statement

ADR-003 made an agent's toolset a call-site decision and introduced `AgentToolSelection`. ADR-005 then
replaced literal name lists with a rule-based *domain vocabulary*: 40 domains, each owning a set of
name-matching rules, plus a separate role-toolset table that mapped `AgentPreset` to domains.

ADR-005 was rejected after review for four reasons.

1. **The mapping was never declared, only inferred.** A domain carried a `Name` and a set of `Rules` as
   two independent literals. Nothing tied `AgentToolDomainCatalog.Registry` (named `"Registry"`) to the
   rule `Area("Registry")`. Sixteen of forty domains ended up with mismatched strings, and the only
   thing that decided which domain a tool belonged to was a linear scan of the rules in declaration
   order. There was no place in the codebase where "prefix → group" was stated.
2. **The vocabulary still required hardcoded name fragments.** Thirty-six `Token(...)` fragments were
   spread across twenty-two domains, plus four hardcoded local tool-name literals. The claim that new
   or renamed tools would be discovered automatically was only true for the subset of domains that
   happened to be expressed with `Area(...)`.
3. **Two axes of assignment competed.** The preset had an implicit role toolset *and* the call site
   passed an `AgentToolSelection`. `FromRole(AgentPreset.WorkerAgent)` appeared at call sites, which
   meant the call site had to know the preset in order to ask for the preset's own tools. That is
   circular, and it made the call-site parameter mandatory in practice rather than an override.
4. **The granularity was unmanageable.** Forty domains with overlapping and over-broad rules
   (`Area("Web")` and `Area("Search")` claimed most of the catalogue) were far more surface than the
   system needed. Ten to twelve groups is a division a human can hold in their head.

The requirement is a mechanism where:

- A preset **owns** its tool assignment and gets it automatically.
- The call-site parameter is an **optional override**, not the primary mechanism.
- Adding, renaming, or removing a tool on the server requires **no application change**.
- There is exactly **one** place where "this prefix means this group" is written down.

---

### 2. Decision

**A tool's group is determined solely by its name prefix. Each group owns exactly one prefix.**

#### 2.1 Name convention

```
sc_<groupprefix>_<verb>_<noun>
```

- `sc_` is the platform root. `AgentToolGroup.NamePrefixRoot`.
- `<groupprefix>` is the group's single, lowercase, unique segment.
- Comparison is `OrdinalIgnoreCase`; casing in a tool name is never significant.
- The prefix is terminated by `_`, so `sc_forensics_` cannot claim `sc_forensicsx_...`.

#### 2.2 Groups

A group is a `AgentToolGroup`: a `Name`, a `Description`, and a `Prefix`. The constructor rejects a
prefix that lacks the `sc_` root, lacks the terminating underscore, or names no group segment (a bare
`sc_` would claim every prefixed tool).

`AgentToolGroupCatalog` is the single registry. It holds fourteen groups:

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

The catalogue is validated by a static constructor that throws on a duplicate name, a duplicate
prefix, or a **nested** prefix. Nesting matters: if both `sc_a_` and `sc_a_b_` were registered, the
tool `sc_a_b_do_thing` would be claimed by two groups, and membership would depend on evaluation
order instead of on the name alone. The guard makes that a startup failure rather than a silent
ambiguity.

#### 2.3 Resolution

```
preset.ToolGroups  →  group names  →  prefixes  →  matching tools     (default)
call-site AgentToolSelection                                          (override, replaces entirely)
```

`AgentPresetDefinition` gained `IReadOnlyList<string> ToolGroups`. When `CreateAgentAsync` is called
without a selection, `SentinelAgentFactory.ResolveTools` resolves the preset's group names. When a
selection is supplied, it **replaces** the preset default; it does not union with it.

#### 2.4 Selection kinds

| Kind | Meaning |
| --- | --- |
| `None` | No tools. |
| `All` | Every tool claimed by a **registered prefix**. Uncategorized tools are excluded by design. |
| `Named` | Explicit tool names. |
| `Groups` | Group names, resolved through their prefixes. |

`FromRole` and `FromDomains` are deleted. Role-based assignment is expressed by *choosing the preset*,
which is the only thing that should carry role semantics.

There is deliberately no `ByRole` selector: a call site that wants role tools should name the preset.

#### 2.5 Uncategorized

Any tool whose name matches no registered prefix is **uncategorized**. It is excluded from `All` and
from every group. It remains reachable by explicit name. `AgentToolGroupCatalog.Uncategorized` is the
safety net; the factory logs the count on every tool-cache refresh and `IModelConfigGate` reports it.

Until the MCP server's tools adopt the convention, **every** tool is uncategorized. That is the
expected and accepted state during development.

#### 2.6 Failure contract

| Condition | Behaviour |
| --- | --- |
| A preset or call site names a group that is not registered | **Throw** `InvalidOperationException` naming the group and listing the valid names |
| A call site names a tool that is not in the catalogue | **Log warning**, bind the names that did resolve, continue |
| A registered group claims zero live tools | **Log warning**, bind nothing, continue |
| A tool matches no registered prefix | Uncategorized; logged, excluded from `All`, reachable by name |

An unknown *group* is a configuration defect in code, so it fails fast. An unknown *tool name* is a
statement about a server that changes without notice, so it must not block. An empty *group* is
normal while the server has not adopted the convention, so it is a soft warning.

#### 2.7 Ordering requirement

Prefix matching requires the tool catalogue to be populated **before** any agent is created.
This is already satisfied and required no change:

- `McpServerRegistry.StartAsync` performs `ConnectAsync` → `ListToolsAsync` → publish `ToolNames` →
  `RefreshToolCacheAsync`, and does not return until that sequence completes.
- `AISystemsInitialization.ExecuteAsync` awaits MCP start before `InitializeOrchestrationAsync`.
- `InitializeOrchestrationAsync` → `OrchestrationControl.InitializeAsync` → `CreateAgentAsync`.

The factory additionally subscribes to `ToolCacheChanged` and refreshes its cache, so a late server
does not leave stale tools bound.

#### 2.8 Readiness gate

The gate is unchanged in structure. `IModelConfigGate` already blocks on Ollama unavailability and on
any registered MCP server that is not `Connected`, and already reports per-server warnings. ADR-006
only changes the wording of the uncategorized warning. Because the MCP server feature does not ship
with a consumer-facing server-add UI in this release, a failed internal server produces a visible
warning and a degraded agent — not a terminal failure.

---

### 3. Consequences

**Positive**

- One declaration per group. There is no place where a prefix and a group name can disagree.
- Adding, renaming, or removing a server tool requires no application change, provided the tool carries
  a registered prefix.
- The preset is self-describing: reading `AgentPresetDefinition` tells you what an agent can do.
- The call-site parameter is genuinely optional, so a preset can be reused without a second preset.

**Negative / accepted**

- The MCP server must adopt the convention. Until it does, tools are uncategorized and agents are
  crippled. This is accepted during development; the tool owner will rename.
- A tool placed in the wrong group cannot be corrected from the application side; it must be renamed.
  This is the intended trade: the name is the contract.
- `All` is a debug catch-all. It is not a supported production configuration.

**Neutral**

- `AgentToolDomain`, `ToolMatchRule`, `AgentToolDomainCatalog`, and `AgentToolRoleToolsets` are deleted.
  ADR-005 is superseded.

---

### 4. Alternatives Considered

| Alternative | Rejected because |
| --- | --- |
| Keep rule-based domains, add an explicit name-to-prefix map | Retains two sources of truth and forty groups for a ten-group problem |
| Derive the group from the first `_` segment of the tool name | No platform root, so any server's naming scheme collides with ours |
| Allow multiple prefixes per group | Breaks the partition; a tool could belong to two groups and the winner would depend on scan order |
| Union the override with the preset default | Makes it impossible to *reduce* an agent's toolset from the call site |
| Treat an unknown group as a soft warning | A typo in a preset would silently produce a tool-less agent, which is exactly the invisible failure this ADR exists to prevent |

---

### 5. References

- `projects/SentinelCore.Orchestrations/Abstractions/AgentToolGroup.cs`
- `projects/SentinelCore.Orchestrations/Abstractions/AgentToolGroupCatalog.cs`
- `projects/SentinelCore.Orchestrations/Abstractions/AgentToolSelection.cs`
- `projects/SentinelCore.Orchestrations/Agents/AgentPresets/AgentPresetDefinition.cs`
- `projects/SentinelCore.Orchestrations/Agents/SentinelAgentFactory.cs`
- `docs/ToolingComponent.md`
- ADR-003 (Agent Tool Selection) — still authoritative for the selection value and factory overloads
- ADR-005 (Tool-Domain Vocabulary) — superseded by this record
