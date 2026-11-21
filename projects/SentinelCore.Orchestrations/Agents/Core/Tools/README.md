# Sentinel Core Tools for Orchestration Agents

This folder contains in-process tools registered by `SentinelCore.Orchestrations`, including Microsoft Learn documentation access, agent spawning, and case-engine functions.

- `MsLearnMcpTools` provides Microsoft Learn documentation search and retrieval (`sc_knowledge_search_docs`, `sc_knowledge_fetch_docs`, `sc_knowledge_search_code_samples`) over the Microsoft Learn MCP endpoint. This is a temporary implementation with a mandatory re-design; see ADR-007. It must not ship in the SentinelCore-MCP server.
- `AgentSpawnTools` exposes `sc_internal_spawn_agent`: it spawns a transient one-shot agent from the `UtilityAgent` preset with a caller-supplied `AgentToolSelection.FromGroups(...)`, runs a single task, and returns the agent's answer. The spawned agent is not retained.
- `CaseEngineTools` exposes case creation, final-report insertion, and pattern-summary insertion through registered `AIFunction` values.
- `ToolResult` is the universal result type every tool returns (`ToolResult.Ok` / `ToolResult.Fail`).
- The full operating-system diagnostic toolset is expected to come from external MCP servers and is not implemented in this folder.
- Agent access is selected through the preset's `ToolGroups` (the `Internal` and `Knowledge` groups for these tools) or a call-site `AgentToolSelection` override. Tool selection does not itself represent authentication or authorization.

See [ToolingComponent.md](../../../../../docs/ToolingComponent.md) for current catalog and selection behavior.

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 2.0.0 | unknown | unknown | Replaced the claim that all tools moved to MCP with the current local case and documentation tool ownership. |
| 3.0.0 | 2026-11-18 | Kyle | Replaced `MsDocsMcpServerTool` with the consolidated `MsLearnMcpTools` (Knowledge group, ADR-007) and added `AgentSpawnTools` (spawn tool, Internal group). |
