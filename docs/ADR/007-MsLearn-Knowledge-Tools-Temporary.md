# ADR-007: MS Learn Knowledge Tools as a Temporary In-Process Implementation

**Status:** Accepted

**Date:** 2026-11-18

## Context

The Core agent needs Microsoft Learn documentation search and retrieval to ground its answers in authoritative product documentation. Previously the platform bound uncategorized third-party Microsoft Learn MCP server tools whose names are not ours to rename. Because those names are foreign to the tool naming convention defined in [ADR-006](006-Tool-Group-Prefix-Scheme.md), they could not be claimed by any registered group and were reachable only by explicit name.

The tooling design document (ToolingComponent.md v3.1) mandated replacing those bindings with internal HTTP-based tools placed in a knowledge group. The same document classifies this implementation as temporary with a mandatory re-design, and explicitly states that these tools must NOT ship in the SentinelCore-MCP server.

## Decision

A single sealed plain class, `MsLearnMcpTools` (namespace `SentinelCore.Orchestrations.Agents.Core.Tools`), hosts three methods — `SearchDocs`, `FetchDocs`, and `SearchCodeSamples` — exposed as three `AIFunction` registrations created with `AIFunctionFactory.Create`:

- `sc_knowledge_search_docs`
- `sc_knowledge_fetch_docs`
- `sc_knowledge_search_code_samples`

All three tools belong to the `Knowledge` tool group.

Implementation details:

- Each call posts a JSON-RPC `tools/call` payload to the Microsoft Learn MCP endpoint. The endpoint is read from the `MSLEARN_MCP_ENDPOINT` environment variable and defaults to `https://learn.microsoft.com/api/mcp`.
- The MCP response envelope is NOT parsed; the raw response body is returned as the ToolResult payload.
- Every call is wrapped in a `ToolResult` (Ok/Fail) per the platform tool contract.
- The class deliberately derives from nothing (no `AITool` base) — it is a method host, registered in DI as `Singleton`, with `AITool` wrappers created per function.

## Rationale

- The group prefix scheme ([ADR-006](006-Tool-Group-Prefix-Scheme.md)) lets these tools be grouped fluidly without hardcoding names.
- Keeping the implementation deliberately unpolished (no envelope parsing, no error normalization beyond `ToolResult`) avoids investing in an external API that may change or break without notice.

## Consequences

- (a) A re-design is MANDATORY in the future: proper MCP client integration or a stable HTTP wrapper with envelope parsing. This ADR is the tracking record for that obligation.
- (b) The tools are brittle and may break without notice; failures surface as `ToolResult.Fail`.
- (c) The three tools must never be added to the SentinelCore-MCP server project.
- (d) No new NuGet dependencies were introduced (`System.Net.Http.Json` only).
- (e) The CoreAgent preset now includes the `Knowledge` group.

Use this document when evaluating, changing, or re-designing the Microsoft Learn knowledge tools, and when reviewing what may or may not ship in the SentinelCore-MCP server.

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 1.0.0 | unknown | unknown | Initial version |
