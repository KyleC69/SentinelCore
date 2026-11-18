## Architecture Decision Record: Rule-Based Tool-Domain Vocabulary (ADR-005)

**Status:** Accepted
**Date:** 2026-02-20
**Architects:** [Agent/Copilot]
**Impacted Components:** `SentinelCore.Orchestrations` (`Abstractions/`, `Agents/`, `Workflows/`, `Infrastructure/DI/`), `SentinelCore.UI` (`Services/`), `SentinelCore.Tests`

---

### 1. Context and Problem Statement

ADR-003 gave every call site a first-class toolset selection and left `FromNames` as the only way to
describe a *group* of tools. That is correct for a single, stable tool name and wrong for everything
else. Two problems surfaced immediately:

1. **Brittleness against catalogue churn.** The live MCP catalogue is 140 tools across ~50
   capability areas and grows every time a server is added. Any assignment expressed as a literal
   name list silently loses a tool the moment a name is added, renamed, or pluralised. The failure is
   invisible: the agent keeps working with a smaller toolset than the author intended.
2. **The assignment did not express intent.** A call site that wanted "the agent that investigates
   file-system and registry evidence" had to enumerate names. Nothing in the code said *file system*
   or *registry*, so the assignment could not be reviewed, diffed, or reasoned about.

An earlier design introduced a fixed `ToolDomain` enum plus `DomainToolNames` and `RoleToolNames`
literal tables. That design was rejected because it reproduced the same problem one level up: the
literal tables were still name lists, and adding a domain required editing an enum, a `switch`, and a
table in three places.

The goal: let a call site name the *kind of evidence* an agent works with, and have the factory
derive the tool names from the live catalogue at construction time, so that new tools are picked up
automatically and a renamed tool does not silently disappear.

### 2. Decision

Introduce a rule-based domain vocabulary in
`SentinelCore.Orchestrations.Abstractions.AgentToolSelection.cs`. No tool name is ever written down.

#### 2.1 A domain is a set of name *rules*, never a list

`AgentToolDomain` is a record carrying a name, a description, and `IReadOnlyList<ToolMatchRule>`.
`Matches(string toolName)` is a pure predicate. `SelectFrom(IEnumerable<string>)` applies it.

A domain therefore describes *shape*, not membership. A tool added tomorrow that follows the area
naming convention joins its domain with no code change.

#### 2.2 Exactly two matchers

| Rule | Semantics |
| ---- | --------- |
| `ToolMatchRule.Prefix(stem)` | The tool name starts with `stem + "_"`. `Prefix("Power")` yields `Power_` and does **not** match `PowerShell_`. |
| `ToolMatchRule.Token(stem)` | Some underscore-delimited segment of the name equals `stem`, case-insensitively. |

`Token` is what makes the vocabulary vendor-independent and pluralisation-tolerant: the segment
`registry` matches `registry_read_value` and `Sysinternals_Autoruns_Registry_Keys` alike, without the
vocabulary knowing that Sysinternals exists. There is deliberately **no** `Exact` rule (that is what
`FromNames` is for) and **no** `Suffix` rule (nothing in the catalogue is suffix-keyed, and it would
reintroduce the fragility).

Both matchers return `false` for `null`/empty input. Neither throws. A rule that matches nothing is a
valid, empty domain.

#### 2.3 `Area` derives the conventional rule pair

`AgentToolDomainCatalog` builds each domain through a private `Area(stem, extra…)` helper that emits
`Prefix(stem)` and, when `stem` contains no underscore, also `Token(stem)`. So `Area("Registry")`
claims both `Registry_*` and any name carrying a `registry` segment, while `Area("Event_Log")` and
`Area("local_case")` emit the prefix only, because their stems are already multi-segment.

#### 2.4 Forty domains and twelve composite categories

`AgentToolDomainCatalog.All` holds the 40 leaf domains (`Registry`, `EventLog`, `FileSystem`,
`Process`, `Service`, `Driver`, `ScheduledTasks`, `Network`, `Firewall`, `Dns`, `Certificates`,
`Bitlocker`, `Defender`, `Security`, `Auditing`, `Permissions`, `Uac`, `Tokens`, `Logon`, `Sessions`,
`GroupPolicy`, `Wmi`, `System`, `Memory`, `Modules`, `Pipes`, `PowerShell`, `Com`, `Autoruns`,
`ShellTaskbar`, `Devices`, `Driver`, `Volume`, `Hotfix`, `Software`, `SearchIndex`, `Browser`,
`AllowedCommands`, `CaseLifecycle`, `Documentation`, `WebResearch`).

`AgentToolDomainCatalog.Categories` holds composites (`Forensics`, `Persistence`, `Identity`,
`NetworkAccess`, `Cryptography`, `Configuration`, `Execution`, `MemoryInspection`, `Hardware`,
`Hardening`, `CaseWork`, `Utilities`). A category **is** an `AgentToolDomain` whose rules are the
union of its members' rules, so it flows through the same resolution path. Categories add no new
selection kind and no new code path.

Overlap between domains is intentional and is resolved by de-duplicating on `AITool.Name`
(`OrdinalIgnoreCase`) when the factory binds the toolset. A registry ACL read legitimately belongs to
both `Registry` and `Permissions`.

`Sysinternals` is not a domain. A vendor name is not an area of the operating system; Sysinternals
tools are categorised by the purpose of each tool, alongside their non-Sysinternals peers.

#### 2.5 Roles are named toolsets, not name lists

`AgentToolRoleToolsets` maps an `AgentPreset` to the domains that role is allowed to use:

| Role | Toolset |
| ---- | ------- |
| `CoreAgent` | `CaseLifecycle`, `Documentation`, `WebResearch` |
| `WorkerAgent` | `CaseWork` (file system, registry, event log, process, service, driver, scheduled tasks) |
| `UtilityAgent` | `Utilities` (system, search index, browser, volume) |
| `SafetyAgent` | `Guardrails` (security, auditing, permissions, Defender, firewall, Group Policy, UAC) |
| `AggregatorAgent`, `ReportExtractorAgent` | `EvidenceVerification` (`Documentation`) |
| `ManagerAgent`, `ClassifierAgent` | empty — the Manager is tool-free by PL-3; the Classifier only routes |

The Core agent is the human interface: it answers case questions and general knowledge, and
delegates deeper status checks to workers so its context stays free of tool output. It therefore holds
no operating-system tools, only case lifecycle, documentation lookup, and (when available) web
research.

#### 2.6 An omitted selection resolves to the role toolset

`SentinelAgentFactory.ResolveTools` takes the `AgentPreset` and the optional selection:

```text
selection == null  -> AgentToolRoleToolsets.For(preset); [] when the role is tool-free
selection.Kind     -> None | All | Named | Domains
```

The preset definition no longer carries a `UseTools` flag. A boolean "has tools" axis could not
express *which* tools and produced an empty toolset for every preset once the flag stopped being set.
Role is a property of the agent kind, so it belongs on the enum, not on a per-preset bool.

#### 2.7 `All` is a debug catch-all with zero filters

`AgentToolSelection.All` resolves to the union of `IMcpServerRegistry.GetToolsFromCache()` and the
DI-registered local `AITool` catalog, with no domain, role, or prefix filter applied. It therefore
includes tools from **any** MCP server loaded at startup, including servers added after the code was
written. `All` is not a production assignment; it exists so a diagnostic workflow
(`CustomGroupWorkflow`) can be handed everything.

#### 2.8 `ByName` remains, and fails loudly

`FromNames` is still the escape hatch for a genuinely specific requirement. An unresolvable name
throws `InvalidOperationException` from the factory, which blocks that agent's feature. It is not a
process-level or terminal failure.

### 3. Readiness Gate

Tool assignment is a call-site decision, so it can produce three classes of problem. They are
surfaced through `SentinelCore.UI.Services.IModelConfigGate`, which drives the `CoreChatViewModel`
banner and `CanSend()`.

| Tier | Condition | Behaviour |
| ---- | --------- | --------- |
| **Blocking** | Ollama is not reachable, **or** any registered MCP server is not `Connected` | `CanSend()` is false and the banner names the failing servers. The same gate that already guarded on Ollama now guards on MCP health. |
| **Warning** | A selected domain or role resolved to zero tools, **or** `AgentToolDomainCatalog.Unclassified` reports catalogue tools no domain claims | Non-blocking banner text. An empty domain is a legitimate state while a server is stopped, so it must not block. |
| **Silent** | `None`, or a narrow selection that resolved cleanly | No banner. |

The gate is global because `Send()` starts the whole master workflow, not just the Core agent; a
degraded toolset anywhere in that workflow is a degraded session.

`AgentToolDomainCatalog.Unclassified` is the structural guarantee against tool loss: any catalogue
tool that matches no rule is reported, so a new naming convention surfaces as a warning instead of
silently shrinking an agent's toolset.

### 4. Consequences

**Positive**

* Adding or renaming a tool cannot silently remove it from a role or domain: matching is by shape.
* A call site states intent (`FromRole(WorkerAgent)`, `FromDomains(CaseWork)`), not a name list.
* One resolution path serves names, domains, categories, roles, and `All`; categories needed no new
  code.
* `Unclassified` turns catalogue drift into a visible warning.
* No enum, `switch`, and table had to be kept in sync.

**Negative / accepted costs**

* **Matching is a heuristic.** A tool whose name does not follow the area convention lands in
  `Unclassified` and is reachable only through `All` or `FromNames`. This is detected and reported,
  not silent, but it is a maintenance obligation: new tools should follow `<Area>_<Verb>_<Object>`.
* **Overlap requires de-duplication.** Binding a category can claim the same tool twice; the factory
  de-duplicates by name, so the cost is a `HashSet`, not a behaviour change.
* **Domain names are not a public contract.** The set of domains may change as the catalogue does.
  Only `AgentPreset` and the selection kinds are stable API.
* `AgentPresetDefinition.UseTools` was removed. The `ToolDomain` enum and the `DomainToolNames` /
  `RoleToolNames` tables are gone; the application is unreleased, so no source compatibility is owed.

### 5. Alternatives Rejected

1. **Extend the `ToolDomain` enum with more members.**
   Rejected: the enum carried no rules, so every new member still required a hand-maintained name
   list, and `ResolveTools` ended in `default: throw`, making an unknown member a runtime failure.
2. **An array of tool groups on `CreateAgentAsync`.**
   Rejected as a *signature* change: the grouping is a property of the selection, not of the factory
   call shape. `FromDomains(params AgentToolDomain[])` expresses the same thing without a fifth
   overload set, and keeps the four selection kinds as the only entry points.
3. **Explicit `Suffix` and `Exact` matchers.**
   Rejected: `Exact` duplicates `FromNames`, and `Suffix` reintroduces the fragility this ADR exists
   to remove.
4. **Make an empty domain a hard failure.**
   Rejected: an MCP server being stopped is an expected state. An empty domain is a warning; only an
   unhealthy server or an unreachable model blocks the session.
5. **Keep per-server `AssignedAgentNames` on the MCP definition.**
   Rejected: two independent axes deciding the same thing. Which agent gets which tool is decided at
   the call site; the server definition carries no agent assignment at all.

### 6. Verification Strategy

* `SentinelCore.Tests/AgentFactoryToolTests.cs` asserts observable behaviour through the
  `FakeChatClient`'s recorded `ChatOptions`, never factory internals:
  * an omitted selection binds the role toolset — the Worker role claims a `registry_*` tool and
	leaves an unrelated local tool unbound;
  * `None` binds zero tools; `All` binds both an MCP tool and a local tool;
  * `FromNames` binds exactly one tool and resolves case-insensitively; an unknown name throws and
	names the missing tool;
  * `FromDomains` collapses duplicates and binds only tools its rules claim;
  * `ManagerAgent` with a non-empty toolset throws (PL-3);
  * construction succeeds with an empty MCP catalogue.
* `dotnet build SentinelCore.slnx` must report zero errors and
  `dotnet test SentinelCore.Tests` must show no failures beyond the pre-existing suite baseline.
* Every workflow call site in `TheCoreWorkflow`, `CustomGroupWorkflow` and `ConcurrentGroupTeam`
  states its toolset through a role or an explicit selection, so the assignment is reviewable without
  reading the factory.

### 7. Related

* `docs/ADR/003-Agent-Tool-Selection.md` — the selection type this ADR extends.
* `docs/ToolingComponent.md` — the component reference for the vocabulary.
* `architecture/pattern-lock.md` PL-3 — the Manager agent must not have tools.
