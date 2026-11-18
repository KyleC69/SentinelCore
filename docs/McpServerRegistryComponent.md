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
**Consumers:** `SentinelAgentFactory` (tool injection), `SentinelCore.UI` (active WPF host and management pages)

---

## Purpose

The MCP Server Registry lets end users register external **Model Context Protocol (MCP)** servers whose tools become available to Sentinel agents. Servers are managed from a WPF admin page (add/remove, start/stop, status monitoring).

A server definition carries **no per-agent assignment**. Which tools an agent receives is decided at agent creation time through `AgentToolSelection` and the tool-domain vocabulary (see `ToolingComponent.md` and ADR-005). The registry's only job is to publish the tools; the factory decides who gets them.

**Key Principle:** MCP tools flow through the same `AITool` pipeline as local domain tools. `McpClientTool` is wrapped as `McpToolTimeoutFunction` before registry publication so invocation timeout and structured failure behavior are consistent.

---

## Architecture Position

```
┌────────────────────────────────────────────────────────────────────┐
│                     SentinelCore.UI (WPF)                          │
│  McpServersPage / McpServersViewModel                              │
│  • add / remove / start / stop / refresh                           │
│  • status + tool list display                                      │
└───────────────────────────────┬────────────────────────────────────┘
                                │ IMcpServerRegistry
                                ▼
┌────────────────────────────────────────────────────────────────────┐
│                  SentinelCore.Orchestrations                        │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  McpServerRegistry (IMcpServerRegistry)                      │  │
│  │  • RegisterAsync / RemoveAsync / StartAsync / StopAsync      │  │
│  │  • ListAsync / GetAsync                                      │  │
│  │  • GetToolsFromCache / GetServersSnapshot (readiness)        │  │
│  │  • LoadPersistedAsync (called from App startup)              │  │
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
                                │ GetToolsFromCache() + local AIFunction tools
                                │
┌───────────────────────────────┴────────────────────────────────────┐
│  SentinelAgentFactory.ResolveTools(presetKind, toolSelection)       │
│  narrows the merged catalog to the selected tool domains            │
└────────────────────────────────────────────────────────────────────┘
```

---

## 1. Contracts (`SentinelCore.Contracts/Mcp`)

| Type | Purpose |
| ------ | --------- |
| `IMcpServerRegistry` | Registry surface: register/remove/start/stop/list/get, `GetAllToolsAsync`, `GetToolsFromCache`, `GetServersSnapshot` |
| `IMcpServerRegistryStore` | Persistence abstraction (load/save definitions) |
| `McpServerDefinition` | Immutable server config: id, display name, transport, command/endpoint, args, working dir, env vars, OAuth settings |
| `McpServerInfo` | Runtime snapshot: definition + status + tool names + last error |
| `McpServerStatus` | Lifecycle enum: `Stopped`, `Starting`, `Connected`, `Error` |
| `McpServerTransportType` | `Stdio` or `Http` |
| `McpOAuthSettings` | Client id, authorization/token endpoints, scopes |

**Selection semantics:** the registry never filters tools by agent. `GetToolsFromCache()` returns every wrapped tool from every connected server, and `SentinelAgentFactory` narrows that set using the `AgentToolSelection` supplied at the call site — `None`, `All`, `Named` from `FromNames(...)`, or `Domains` from `FromDomains(...)`. `All` applies no filter at all, so any server loaded at startup contributes to it.

---

## 2. Implementation (`SentinelCore.Orchestrations/Mcp`)

| Type | Purpose |
| ------ | --------- |
| `McpServerRegistry` | In-memory registry keyed by server id; persists on register/remove; swallows per-server tool-list failures with logging |
| `McpConnectionFactory` | Builds `McpClient` via the C# SDK: stdio (`StdioClientTransport`) or streamable HTTP (`HttpClientTransport`); performs OAuth when configured |
| `LoopbackOAuthCallbackHandler` | Temporary `HttpListener` on a loopback port + system browser launch for the auth-code + PKCE flow |
| `DpapiTokenCache` | DPAPI-encrypted access/refresh token cache (`mcp-tokens.bin` next to the registry file) |
| `JsonFileMcpServerRegistryStore` | JSON persistence at `%APPDATA%\SentinelCore\mcp-servers.json` (override with `SENTINEL_MCP_REGISTRY_PATH`) |
| `McpToolTimeoutFunction` | `DelegatingAIFunction` that cancels a call after 30 s and returns `ToolResult.Fail` (never throws); success returns `ToolResult.Ok` (see ADR-002) |

### Tool enumeration

Failures are logged and skipped so one broken server cannot starve an agent of its other tools.

### Timeouts (ADR-002)

No MCP wait is unbounded: initialize handshake 30 s (`McpClientOptions.InitializationTimeout`), `StartAsync` 60 s (a timeout sets status `Error` and throws `TimeoutException`; a half-opened client is disposed), per-server tool listing 30 s (server skipped), and each tool invocation 30 s via `McpToolTimeoutFunction`. Caller cancellation is never converted into a timeout.

---

## 3. Agent Integration (`SentinelAgentFactory`)

`SentinelAgentFactory` takes `IMcpServerRegistry` in its constructor and merges registry tools with the DI-supplied local `AIFunction` tools into a single catalog keyed by tool name:

```csharp
Dictionary<string, AITool> catalog = new(StringComparer.OrdinalIgnoreCase);
foreach (AITool tool in _builtToolCache) catalog[tool.Name] = tool;
foreach (AITool tool in _localTools) catalog[tool.Name] = tool;
```

`ResolveTools(presetKind, toolSelection)` then narrows that catalog:

| Selection | Tools bound |
| --------- | ----------- |
| `null` | The role toolset for `presetKind` (`AgentToolRoleToolsets.For`), or none for a tool-free role |
| `None` | None |
| `All` | The entire catalog — every MCP server loaded at startup plus every local tool, unfiltered |
| `Named` (`FromNames(...)`) | Exactly the named tools; an unknown name throws `InvalidOperationException` (feature block) |
| `Domains` (`FromDomains(...)`) | The union of tools claimed by the supplied domains' match rules |

Because resolution is centralized in the factory, **every** agent built through `ISentinelAgentFactory` receives its toolset from the same vocabulary. Adding a tool to an MCP server requires no code change: domain rules match names by prefix and token, so new tools are claimed automatically. Anything unclaimed is logged by the factory; complete UI warning surfacing should not be assumed.

---

## 4. UI (`SentinelCore.UI`)

| Type | Purpose |
| ------ | --------- |
| `McpServersViewModel` | View-model: add/remove/start/stop/refresh commands, add-server form (display name, transport, command/endpoint, args, working dir, env vars), status/tool display, `INavigationAware` + `IDisposable` |
| `McpServersPage` | WPF page with toolbar, collapsible add-server form (stdio-only fields disable when HTTP transport is selected), and a `DataGrid` of `McpServerRow` entries with a color-coded status indicator (green = connected, amber = starting, red = error, muted = stopped) |
| `McpServerRow` | Display model: id, display name, transport, command/endpoint, status, tool names, last error |

Navigation is wired through `SentinelCoreUIServiceExtensions` (page mapping + ViewModel registration) and the `MainWindow` radio-button tab set ("MCP Servers").

---

## 5. DI Composition

`AddSentinelCore` (Orchestrations) registers:

- `IMcpServerRegistry` → `McpServerRegistry` (singleton)
- `IMcpServerRegistryStore` → `JsonFileMcpServerRegistryStore` (singleton)
- `IMcpConnectionFactory` → `McpConnectionFactory` (singleton)

There is no hosted-service initializer: `App.xaml.cs` calls `LoadPersistedAsync` during startup so the
UI owns the lifecycle and nothing is auto-started behind the user's back.

`AddSentinelCoreUi` (UI) registers `McpServersViewModel` and maps `McpServersPage`.

---

## 6. Testing

| Test file | Coverage |
| ----------- | ---------- |
| `McpServerRegistryTests` | Registration, removal, persistence restore, get/list, tool enumeration across connected vs stopped servers, stop transitions. Uses a `FakeMcpClient` subclass of `McpClient` whose `SendRequestAsync` returns a canned `tools/list` JSON-RPC result — the SDK's non-virtual `ListToolsAsync` pipeline deserializes it into real `McpClientTool` instances. |
| `McpServersViewModelTests` | Constructor null-guards, command gating (add/remove), add/remove/start/stop command interactions with a mocked registry, refresh loading of servers, double-dispose safety. |
| `AgentBuilderTests` | `SentinelAgentFactory` constructor guards and MCP registry injection via a `FakeMcpServerRegistry`. |

**Note on `FakeMcpClient`:** the `McpClient` constructor is an experimental SDK extensibility API (`MCPEXP002`); the test file suppresses the diagnostic with a file-level `#pragma warning disable MCPEXP002`.

---

## 7. Pattern-Lock Compliance

| Rule | Status | Notes |
| ------ | -------- | ------- |
| Real SDK types only | ✅ | `McpClient` and `McpClientTool`; registry tools are wrapped by the `McpToolTimeoutFunction` `AIFunction` adapter for timeout/result handling |
| Centralized tool resolution | ✅ | `SentinelAgentFactory.ResolveTools` owns every tool decision |
| Persistence overridable | ✅ | `SENTINEL_MCP_REGISTRY_PATH` |
| UI stays in control of lifecycle | ✅ | `App.xaml.cs` loads persisted servers; nothing auto-starts |
| No per-agent assignment on the server | ✅ | Assignment is a call-site concern (`AgentToolSelection`) |
| Failures isolated per server | ✅ | Tool-list errors logged, not thrown |

---

## 8. Open Items / TODOs

| Item | Location | Status |
| ------ | ---------- | -------- |
| Token refresh on expiry | `McpConnectionFactory` | ⚠️ Refresh token stored; proactive refresh not implemented |
| Server health polling | `McpServerRegistry` | ❌ Status only updates on explicit start/stop/refresh |
| Readiness gate | `IModelConfigGate` | ✅ A server not `Connected` blocks agent actions and raises the UI banner (ADR-005) |

---

## 9. Related Documentation

| Document | Description |
| ---------- | ------------- |
| `ToolingComponent.md` | Rule-based tool-domain vocabulary and role-based toolsets |
| `ADR/003-Agent-Tool-Selection.md` | Call-site toolset selection |
| `ADR/005-Tool-Domain-Vocabulary.md` | Domains as match rules; readiness gate |
| `OrchestrationComponent.md` | Agent construction pipeline |
| `ContractsComponent.md` | Shared abstractions |
| `user/ConfigurationAndCustomization.md` | Environment variable overrides |

---

## Version History

| Version | Date | Author | Changes |
| --------- | ------ | -------- | --------- |
| v1.0 | 2026-09-09 | Kyle | Initial documentation for the MCP server registry feature |
| 1.0.1 | unknown | unknown | Updated the consumer host name from the retired shell to `SentinelCore.UI`. |
| v1.1 | 2026-02-20 | Kyle | Removed per-agent assignment (`AssignedAgentNames`, `ISentinelAgentCatalog`, `AgentAssignmentRow`, `GetToolsForAgentAsync`, assignment editor UI). Tool assignment is now a call-site decision through `AgentToolSelection`. Corrected stale implementation/DI tables. |
