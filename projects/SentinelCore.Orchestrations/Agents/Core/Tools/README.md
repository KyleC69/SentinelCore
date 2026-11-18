# Sentinel Core Tools for Orchestration Agents

This folder contains in-process tools registered by `SentinelCore.Orchestrations`, including documentation access and case-engine functions.

- `MsDocsMcpServerTool` provides documentation search through the local MCP client/tool surface.
- `CaseEngineTools` exposes case creation, final-report insertion, and pattern-summary insertion through registered `AIFunction` values.
- The full operating-system diagnostic toolset is expected to come from external MCP servers and is not implemented in this folder.
- Agent access is selected through `AgentToolSelection` and the tool-domain vocabulary. Tool selection does not itself represent authentication or authorization.

See [ToolingComponent.md](../../../../../docs/ToolingComponent.md) for current catalog and selection behavior.

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 2.0.0 | unknown | unknown | Replaced the claim that all tools moved to MCP with the current local case and documentation tool ownership. |
