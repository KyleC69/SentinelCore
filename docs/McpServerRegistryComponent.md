---
title: "MCP Server Registry Component"
status: Active
component: Mcp
last_updated: 2026-09-09
version: v1.0
---

# SentinelCore MCP Server Registry Component

**Projects:** `SentinelCore.Contracts` (contracts), `SentinelCore.Orchestrations` (implementation), `SentinelCore.UI` (management page)
**Namespaces:** `SentinelCore.Mcp` (contracts), `SentinelCore.Orchestrations.Mcp` (implementation), `SentinelCore.UI.ViewModels` / `SentinelCore.UI.Views` (UI)
**Dependencies:** `ModelContextProtocol` 2.1.0 (C# SDK), `Microsoft.Extensions.AI`, `Microsoft.Extensions.Hosting.Abstractions`
**Consumers:** `SentinelAgentFactory` (tool injection), `SentinelCoreAdmin` (WPF shell)

---

## Purpose

The MCP Server Registry lets end users register external **Model Context Protocol (MCP)** servers whose tools become available to Sentinel agents. Servers are managed from a WPF admin page (add/remove, start/stop, status monitoring) and can be **assigned to specific agents** — a server assigned to `CoreChat` only exposes its tools to that agent, while an unassigned server is available to every agent.

**Key Principle:** MCP tools flow through the same `AITool` pipeline as built-in domain tools. `McpClientTool` inherits from `AIFunction`, so registry tools are injected into agent construction without adapters or wrappers.

---

## Architecture Position

```
┌────────────────────────────────────────────────────────────────────┐
│                     SentinelCore.UI (WPF)                          │
│  McpServersPage / McpServersViewModel                              │
│  • add / remove / start / stop / refresh                           │
│  • status + tool list + assigned agents display                    │
└───────────────────────────────┬────────────────────────────────────┘
                                │ IMcpServerRegistry
                                ▼
┌────────────────────────────────────────────────────────────────────┐
│                  SentinelCore.Orchestrations                        │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  McpServerRegistry (IMcpServerRegistry)                      │  │
│  │  • RegisterAsync / RemoveAsync / StartAsync / StopAsync      │  │
│  │  • ListAsync / GetAsync                                      │  │
│  │  • GetToolsForAgentAsync(agentName) → IReadOnlyList<AITool>   │  │
│  │  • LoadPersistedAsync (hosted initializer)                   │  │
│  └──────────────┬───────────────────────────────────────────────┘  │
│                 │ IMcpConnectionFactory                             │
│  ┌──────────────▼───────────────────────────────────────────────┐  │
│  │  McpConnectionFactory                                         │  │
│  │  • Stdio transport (command + args + env)                    │  │
│  │  • Streamable HTTP transport (endpoint URL)                  │  │
│  │  • OAuth 2.0 auth-code + PKCE (loopback listener + browser)  │  │
│  │  • DpapiTokenCache (mcp-tokens.bin)                          │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  JsonFileMcpServerRegistryStore (IMcpServerRegistryStore)     │  │
│  │  • %APPDATA%\SentinelCore\mcp-servers.json                    │  │
│  │  • SENTINEL_MCP_REGISTRY_PATH override                        │  │
│  └──────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────┘
                                ▲
                                │ GetToolsForAgentAsync(logicalAgentName)
                                │
┌───────────────────────────────┴────────────────────────────────────┐
│  SentinelAgentFactory.BuildAgentOptionsAsync                        │
│  merges profile.Tools with registry tools for the agent            │
└────────────────────────────────────────────────────────────────────┘
```

---

## 1. Contracts (`SentinelCore.Contracts/Mcp`)

| Type | Purpose |
| ------ | --------- |
| `IMcpServerRegistry` | Registry surface: register/remove/start/stop/list/get + `GetToolsForAgentAsync` |
| `IMcpServerRegistryStore` | Persistence abstraction (load/save definitions) |
| `ISentinelAgentCatalog` | Lists logical agent names for per-agent assignment UI |
| `McpServerDefinition` | Immutable server config: id, display name, transport, command/endpoint, args, working dir, env vars, OAuth settings, assigned agent names |
| `McpServerInfo` | Runtime snapshot: definition + status + tool names + last error |
| `McpServerStatus` | Lifecycle enum: `Stopped`, `Starting`, `Connected`, `Error` |
| `McpServerTransportType` | `Stdio` or `Http` |
| `McpOAuthSettings` | Client id, authorization/token endpoints, scopes |

**Assignment semantics:** `McpServerDefinition.AssignedAgentNames` empty ⇒ available to **all** agents; otherwise only listed agents receive the server's tools (case-insensitive match against the logical agent name passed to `SentinelAgentFactory`).

---

## 2. Implementation (`SentinelCore.Orchestrations/Mcp`)

| Type | Purpose |
| ------ | --------- |
| `McpServerRegistry` | In-memory registry keyed by server id; persists on register/remove; swallows per-server tool-list failures with logging |
| `McpConnectionFactory` | Builds `McpClient` via the C# SDK: stdio (`StdioClientTransport`) or streamable HTTP (`HttpClientTransport`); performs OAuth when configured |
| `LoopbackOAuthCallbackHandler` | Temporary `HttpListener` on a loopback port + system browser launch for the auth-code + PKCE flow |
| `DpapiTokenCache` | DPAPI-encrypted access/refresh token cache (`mcp-tokens.bin` next to the registry file) |
| `JsonFileMcpServerRegistryStore` | JSON persistence at `%APPDATA%\SentinelCore\mcp-servers.json` (override with `SENTINEL_MCP_REGISTRY_PATH`) |
| `McpServerRegistryInitializer` | `IHostedService` that calls `LoadPersistedAsync` at startup; intentionally does **not** auto-start connections so the UI stays in control |
| `SentinelAgentCatalog` | Static list of logical agent names: `CoreChat`, `Classifier`, `TheCore`, `SafetyAgent`, `Manager`, `Worker1`, `Worker2`, `Worker3` |

### Tool enumeration

`StartAsync` connects the client and calls `ListToolsAsync` to capture tool names for display. `GetToolsForAgentAsync` re-enumerates live tools for every connected server the agent is assigned to and returns them as `AITool` (each `McpClientTool` is an `AIFunction`). Failures are logged and skipped so one broken server cannot starve an agent of its other tools.

---

## 3. Agent Integration (`SentinelAgentFactory`)

`SentinelAgentFactory` takes `IMcpServerRegistry` in its constructor. `BuildAgentOptionsAsync` merges the profile's built-in tools with `registry.GetToolsForAgentAsync(profile.AgentName)`:

```csharp
IReadOnlyList<AITool> mcpTools = await _mcpServerRegistry
    .GetToolsForAgentAsync(logicalAgentName, cancellationToken)
    .ConfigureAwait(false);

List<AITool> tools = [.. profile.Tools, .. mcpTools];
```

Because injection is centralized in the factory, **every** agent built through `ISentinelAgentFactory` (CoreChat, workflow agents, dynamic agents) receives its assigned MCP tools without per-agent wiring.

---

## 4. UI (`SentinelCore.UI`)

| Type | Purpose |
| ------ | --------- |
| `McpServersViewModel` | View-model: add/remove/start/stop/refresh commands, add-server form (display name, transport, command/endpoint, args, working dir, env vars, per-agent checkboxes), status/tool/agent display, `UpdateAssignmentsCommand` for the selected server, `INavigationAware` + `IDisposable` |
| `McpServersPage` | WPF page with toolbar, collapsible add-server form (stdio-only fields disable when HTTP transport is selected), a `DataGrid` of `McpServerRow` entries with a color-coded status indicator (green = connected, amber = starting, red = error, muted = stopped), and a per-agent assignment editor for the selected server |
| `McpServerRow` | Display model: id, display name, transport, command/endpoint, status, tool names, assigned agents text, last error |
| `AgentAssignmentRow` | One logical agent in the assignment editors: agent name + checked state (unchecked everywhere = available to all agents) |

Navigation is wired through `SentinelCoreUIServiceExtensions` (page mapping + ViewModel registration) and the `MainWindow` radio-button tab set ("MCP Servers").

---

## 5. DI Composition

`AddSentinelCore` (Orchestrations) registers:

- `IMcpServerRegistry` → `McpServerRegistry` (singleton)
- `IMcpServerRegistryStore` → `JsonFileMcpServerRegistryStore` (singleton)
- `IMcpConnectionFactory` → `McpConnectionFactory` (singleton)
- `ISentinelAgentCatalog` → `SentinelAgentCatalog` (singleton)
- `IHostedService` → `McpServerRegistryInitializer`

`AddSentinelCoreUi` (UI) registers `McpServersViewModel` and maps `McpServersPage`.

---

## 6. Testing

| Test file | Coverage |
| ----------- | ---------- |
| `McpServerRegistryTests` | Registration, removal, persistence restore, get/list, per-agent filtering (universal vs assigned vs stopped), stop transitions. Uses a `FakeMcpClient` subclass of `McpClient` whose `SendRequestAsync` returns a canned `tools/list` JSON-RPC result — the SDK's non-virtual `ListToolsAsync` pipeline deserializes it into real `McpClientTool` instances. |
| `McpServersViewModelTests` | Constructor null-guards, command gating (add/remove), add/remove/start/stop command interactions with a mocked registry, refresh loading of servers and agent names, double-dispose safety. |
| `AgentBuilderTests` | `SentinelAgentFactory` constructor guards and MCP registry injection via a `FakeMcpServerRegistry`. |

**Note on `FakeMcpClient`:** the `McpClient` constructor is an experimental SDK extensibility API (`MCPEXP002`); the test file suppresses the diagnostic with a file-level `#pragma warning disable MCPEXP002`.

---

## 7. Pattern-Lock Compliance

| Rule | Status | Notes |
| ------ | -------- | ------- |
| Real SDK types only | ✅ | `McpClient`, `McpClientTool`, `AIFunction` — no adapters |
| Centralized tool injection | ✅ | `SentinelAgentFactory` merges registry tools |
| Persistence overridable | ✅ | `SENTINEL_MCP_REGISTRY_PATH` |
| UI stays in control of lifecycle | ✅ | Initializer loads but never auto-starts |
| Per-agent assignment | ✅ | `AssignedAgentNames` + case-insensitive match |
| Failures isolated per server | ✅ | Tool-list errors logged, not thrown |

---

## 8. Open Items / TODOs

| Item | Location | Status |
| ------ | ---------- | -------- |
| Per-agent assignment editor UI | `McpServersPage` | ✅ Checkbox editors in both the add form and the selected-server panel |
| Token refresh on expiry | `McpConnectionFactory` | ⚠️ Refresh token stored; proactive refresh not implemented |
| Server health polling | `McpServerRegistry` | ❌ Status only updates on explicit start/stop/refresh |

---

## 9. Related Documentation

| Document | Description |
| ---------- | ------------- |
| `ToolingComponent.md` | Static tool registry and role-based toolsets |
| `OrchestrationComponent.md` | Agent construction pipeline |
| `ContractsComponent.md` | Shared abstractions |
| `user/ConfigurationAndCustomization.md` | Environment variable overrides |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-09-09 | Kyle | Initial documentation for the MCP server registry feature |
