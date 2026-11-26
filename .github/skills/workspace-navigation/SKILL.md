---
name: workspace-navigation
description: Use this skill any time you are navigating this repository. Provides guidance for navigating the SentinelCore workspace and repository structure, including key directories and important files.
user-invocable: false

---

# Workspace Navigation Skill

This skill helps developers quickly locate important parts of the **SentinelCore** solution.

## Workspace Layout

- **Workspace root**: `F:\Solutions\SentinelCore` — contains `SentinelCore.slnx`, `global.json`, and the `.github\` customization folder (instructions, skills, agents).
- **Repository root**: `F:\Solutions\SentinelCore\SentinelCore` — Git root; project source, docs, ADRs, and the pattern lock live here.
- **Projects folder**: `F:\Solutions\SentinelCore\SentinelCore\projects` — contains the in-tree C# project directories of the solution.
- **Out-of-tree solution/folder members** live as siblings of the workspace root (`F:\Solutions\...`). They exist on disk but are **not under** `F:\Solutions\SentinelCore` — do not assume a missing path means a missing project:
  - `F:\Solutions\SentinelCoreSecurity` — `SentinelCore.Security` module (ADR-008); solution member via relative path `../SentinelCoreSecurity/SentinelCore.Security.csproj`.
  - `F:\Solutions\SentinelCore.Tests` — test project folder; **not** currently referenced by `SentinelCore.slnx`.

## Key Files & Components

| Component | Path | Description |
|----------|------|-------------|
| Solution file | `F:\Solutions\SentinelCore\SentinelCore.slnx` | Visual Studio solution (6 in-tree projects + out-of-tree Security) |
| Pattern lock | `F:\Solutions\SentinelCore\SentinelCore\architecture\pattern-lock.md` | Authoritative locked patterns (PL-# rules); read before editing any `.cs` file |
| Orchestrations | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCore.Orchestrations\` | Core engine: agent factory, workflows, executors, safety engine, RAG, MCP connections, DI |
| Contracts | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCore.Contracts\` | Pure DTOs, interfaces, settings, events, MCP registry models; zero dependencies |
| CaseFlowEngine | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCore.CaseFlowEngine\` | `ICaseFlowEngine`, EF Core persistence, migrations, stores |
| MCP Tools | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCore.MCPTools\` | SentinelCore-MCP stdio server — 141 read-only `sc_*` Windows tools (csproj `SentinelCore.MCP.csproj`) |
| UI host | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCore.UI\` | Active WPF composition root (chat, cases, MCP servers, model config) |
| Service host | `F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCoreService\` | Windows Service scaffold |
| Security module | `F:\Solutions\SentinelCoreSecurity\` | Message provenance, identity, crypto, quarantine (out-of-tree) |
| Documentation | `F:\Solutions\SentinelCore\SentinelCore\docs\` | Component docs and `ADR\` decision records |
| Instructions | `F:\Solutions\SentinelCore\.github\instructions\` | `drift-prevention`, `ProjectTerminology`, `ArchitectureGate.md` |
| Skills | `F:\Solutions\SentinelCore\.github\skills\` | `workspace-navigation` (this skill), `create-adr` |
| Agents | `F:\Solutions\SentinelCore\.github\agents\` | WPF UI Agent, integration-architect, regression-Test |

## Usage

Agents can reference this skill when they need to:
- Find the location of a specific project or file.
- Understand the high‑level folder hierarchy.
- Quickly navigate between the workspace root, repository root, and individual project directories.

The skill can be invoked via the **workspace-navigation** skill or directly through the VS Code Copilot UI.

## Reference Maps

- `references/repo-map.md` — detailed per-project file-level maps.
- `references/search-strategies.md` — step-by-step strategies for finding agents, tools, executors, DI registrations, safety rules, middleware, persistence, tests, configuration, and docs.

## Agent Consumption

The following JSON snippet provides a machine‑readable map of the workspace layout for agents that need to resolve paths programmatically:

```json
{
	"workspaceRoot": "F:/Solutions/SentinelCore",
	"repoRoot": "F:/Solutions/SentinelCore/SentinelCore",
	"projectsFolder": "F:/Solutions/SentinelCore/SentinelCore/projects",
	"solutionFile": "F:/Solutions/SentinelCore/SentinelCore.slnx",
	"inTreeProjects": [
		"SentinelCore.CaseFlowEngine",
		"SentinelCore.Contracts",
		"SentinelCore.MCPTools",
		"SentinelCore.Orchestrations",
		"SentinelCore.UI",
		"SentinelCoreService"
	],
	"outOfTreeProjects": [
		{
			"project": "SentinelCore.Security",
			"location": "F:/Solutions/SentinelCoreSecurity",
			"referencedBySolution": true
		},
		{
			"project": "SentinelCore.Tests",
			"location": "F:/Solutions/SentinelCore.Tests",
			"referencedBySolution": false
		}
	],
	"keyFiles": {
		"patternLock": "F:/Solutions/SentinelCore/SentinelCore/architecture/pattern-lock.md",
		"solutionArchitecture": "F:/Solutions/SentinelCore/SentinelCore/docs/SolutionArchitecture.md",
		"toolingDoc": "F:/Solutions/SentinelCore/SentinelCore/docs/ToolingComponent.md",
		"mcpToolingDoc": "F:/Solutions/SentinelCore/SentinelCore/projects/SentinelCore.MCPTools/Docs/ToolingComponent.md",
		"coreToolsReadme": "F:/Solutions/SentinelCore/SentinelCore/projects/SentinelCore.Orchestrations/Agents/Core/Tools/README.md",
		"agentPresetDefinition": "F:/Solutions/SentinelCore/SentinelCore/projects/SentinelCore.Orchestrations/Agents/AgentPresets/AgentPresetDefinition.cs",
		"agentFactory": "F:/Solutions/SentinelCore/SentinelCore/projects/SentinelCore.Orchestrations/Agents/SentinelAgentFactory.cs",
		"diEntry": "F:/Solutions/SentinelCore/SentinelCore/projects/SentinelCore.Orchestrations/Infrastructure/DependencyInjection/SentinelCoreServiceExtensions.cs",
		"adrFolder": "F:/Solutions/SentinelCore/SentinelCore/docs/ADR",
		"terminology": "F:/Solutions/SentinelCore/.github/instructions/ProjectTerminology.instructions.md",
		"driftPrevention": "F:/Solutions/SentinelCore/.github/instructions/drift-prevention.instructions.md"
	}
}
```

Agents can parse this JSON to locate project directories, documentation, and other important resources without relying on human‑readable tables.
