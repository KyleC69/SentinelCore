# SentinelCore MCP Integration

The **SentinelCore.Orchestrations.Mcp** component provides first-class integration with the **Model Context Protocol (MCP)**, enabling SentinelCore agents to discover, connect to, and consume tools exposed by MCP servers.

## Overview

MCP (Model Context Protocol) is an open standard that allows AI applications to connect to external tools and data sources through a unified protocol. This component bridges MCP servers into SentinelCore's agent ecosystem, making MCP tools available as native `AITool` instances that agents can invoke seamlessly.

## Key Features

| Feature | Description |
|---------|-------------|
| **Dual Transport Support** | Connect to MCP servers via `stdio` (local subprocess) or `HTTP` (remote/network) transports. |
| **OAuth Authentication** | Full OAuth 2.0 support for protected HTTP MCP endpoints, including automatic token caching via DPAPI on Windows. |
| **Persistent Registry** | Server definitions are stored in a JSON file and reloaded on startup. |
| **Tool Discovery & Caching** | Automatically discovers tools from connected servers and caches them for low-latency agent access. |
| **Timeout & Fault Tolerance** | Every tool invocation is wrapped with a configurable timeout; failures return structured `ToolResult` objects instead of throwing. |
| **Observable Events** | `ServersChanged` and `ToolCacheChanged` events enable UI or orchestration layers to react to registry mutations. |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore Agent                           │
│  (invokes tools via Microsoft.Extensions.AI AITool interface)  │
└────────────────────────────────┬────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    IMcpServerRegistry                           │
│  (McpServerRegistry implementation)                             │
│  • Register/Start/Stop/List servers                             │
│  • LoadPersistedAsync / PersistAsync                            │
│  • GetAllToolsAsync / GetToolsFromCache                         │
│  • GetServersSnapshot (readiness)                               │
│  • Events: ServersChanged, ToolCacheChanged                     │
└────────────────────────────────┬────────────────────────────────┘
                                 │
              ┌──────────────────┴──────────────────┐
              ▼                                     ▼
┌──────────────────────────────┐      ┌──────────────────────────────┐
│    IMcpConnectionFactory     │      │    IMcpServerRegistryStore   │
│  (McpConnectionFactory impl) │      │ (JsonFileMcpServerRegistry   │
│  • ConnectAsync(definition)  │      │  Store implementation)       │
│  • Creates stdio/HTTP        │      │  • LoadAsync / SaveAsync     │
│    transports with OAuth     │      │  • Atomic write via temp     │
│  • 30s initialization timeout│      │    file + move               │
└──────────────────────────────┘      └──────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      MCP Server(s)                              │
│  • stdio: spawned subprocess (command + args + env + cwd)       │
│  • HTTP:  remote endpoint with optional OAuth                   │
└─────────────────────────────────────────────────────────────────┘
```

## Core Types (Contracts)

All public contracts live in **`SentinelCore.Contracts.Mcp`** and have no implementation dependencies.

| Type | Purpose |
|------|---------|
| `McpServerDefinition` | Immutable configuration for one MCP server (ID, display name, transport, command/endpoint, args, env vars, OAuth, agent allow-list). |
| `McpServerInfo` | Runtime view: definition + current status + discovered tool names + last error. |
| `McpServerStatus` | `Stopped`, `Starting`, `Connected`, `Error`. |
| `McpServerTransportType` | `Stdio` or `Http`. |
| `McpOAuthSettings` | Client ID, redirect URI, scopes for OAuth-protected HTTP servers. |
| `IMcpServerRegistry` | Registry operations (register, start, stop, list, load, get tools, snapshot). |
| `IMcpConnectionFactory` | Creates connected `McpClient` instances from a definition. |
| `IMcpServerRegistryStore` | Persistence abstraction (JSON file implementation provided). |

## Usage

### 1. Register Services (DI)

```csharp
services.AddSingleton<IMcpServerRegistryStore>(sp =>
    new JsonFileMcpServerRegistryStore(
        Path.Combine(appDataPath, "mcp-servers.json"),
        sp.GetRequiredService<ILogger<JsonFileMcpServerRegistryStore>>()));

services.AddSingleton<IMcpConnectionFactory, McpConnectionFactory>();
services.AddSingleton<IMcpServerRegistry, McpServerRegistry>();
```

> **Note**: `McpConnectionFactory` accepts an optional `ITokenCache` for OAuth token persistence. The provided `DpapiTokenCache` uses Windows DPAPI for secure storage.

### 2. Load Persisted Servers at Startup

```csharp
var registry = serviceProvider.GetRequiredService<IMcpServerRegistry>();
await registry.LoadPersistedAsync();
```

### 3. Register a New Server

```csharp
// Stdio server (local subprocess)
var stdioDef = new McpServerDefinition(
    id: "filesystem",
    displayName: "Local Filesystem",
    transportType: McpServerTransportType.Stdio,
    commandOrEndpoint: "npx",
    arguments: ["-y", "@modelcontextprotocol/server-filesystem", "/allowed/path"],
    workingDirectory: null,
    environmentVariables: new Dictionary<string, string> { ["NODE_ENV"] = "production" });

await registry.RegisterAsync(stdioDef);

// HTTP server with OAuth
var httpDef = new McpServerDefinition(
    id: "github",
    displayName: "GitHub MCP",
    transportType: McpServerTransportType.Http,
    commandOrEndpoint: "https://api.github.com/mcp",
    oauthSettings: new McpOAuthSettings(
        clientId: "your-client-id",
        callbackUri: "http://127.0.0.1:54321/callback",
        scopes: "repo read:org"));

await registry.RegisterAsync(httpDef);
```

### 4. Start Servers & Access Tools

```csharp
await registry.StartAsync("filesystem");
await registry.StartAsync("github");

// All tools from all connected servers as AITool instances
IReadOnlyList<AITool> allTools = await registry.GetAllToolsAsync();

// Or use the low-latency cache (populated on each connect/disconnect)
IReadOnlyList<AITool> cachedTools = registry.GetToolsFromCache();

// The factory owns tool resolution; the registry only publishes the tools.
AIAgent agent = await agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.All);
```

### 5. Tool Assignment

A server definition carries no per-agent assignment. Which agent receives a server's tools is decided at
agent creation time through `AgentToolSelection` and the tool-domain vocabulary:

```csharp
// Role toolset: the agent gets the domains its role owns.
var worker = await agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromRole(AgentPreset.WorkerAgent));

// Explicit domains: the agent gets only tools claimed by these rules.
var narrow = await agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.FromDomains(AgentToolDomainCatalog.Registry));

// Debug catch-all: every tool from every loaded MCP server plus the local catalog.
var debug = await agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, AgentToolSelection.All);
```

See [docs/ToolingComponent.md](../../../docs/ToolingComponent.md) for the domain vocabulary and
[ADR-005](../../../docs/ADR/005-Tool-Domain-Vocabulary.md) for the rationale.

### 6. React to Changes (UI / Orchestration)

```csharp
registry.ServersChanged += () => RefreshServerListUI();
registry.ToolCacheChanged += () => RefreshAgentToolCatalog();
```

## Tool Execution Guarantees

Every MCP tool is wrapped in `McpToolTimeoutFunction` which ensures:

- **Timeout**: Default 30 seconds per invocation (configurable per-tool).
- **No Exceptions**: Failures (timeout, transport error, server error) return `ToolResult.Fail(message)`.
- **Success**: Returns `ToolResult.Ok(jsonString)` with the server result serialized to JSON.
- **Cancellation Propagation**: Caller's `CancellationToken` is respected and propagated as `OperationCanceledException`.

## Persistence

- **File**: `mcp-servers.json` (configurable path).
- **Format**: JSON array of `McpServerDefinition` (camelCase property names).
- **Atomic Writes**: Uses temp file + `File.Move(overwrite: true)` to avoid corruption.
- **Load Failure**: Logs error and returns empty list; application continues.

## OAuth Token Cache (Windows)

`DpapiTokenCache` implements `Microsoft.Extensions.AI.ITokenCache` using Windows Data Protection API (DPAPI). Tokens are encrypted per-user and stored in `%LOCALAPPDATA%\SentinelCore\mcp-tokens.dat`.

## Extensibility

| Extension Point | How |
|----------------|-----|
| Custom Persistence | Implement `IMcpServerRegistryStore` and register in DI. |
| Custom Transport | Implement `IMcpConnectionFactory` (rare; stdio/HTTP cover most cases). |
| Per-Tool Timeout | Wrap `McpToolTimeoutFunction` with a different `timeout` value at registration time. |

## Requirements

- **.NET 10+**
- `ModelContextProtocol` NuGet package (client)
- Windows (for DPAPI token cache; other platforms can supply custom `ITokenCache`)

## Related Components

- `SentinelCore.Contracts.Mcp` — Public contracts only (no implementation dependencies).
- `SentinelCore.Orchestrations.Agents` — Agent factory that consumes `IMcpServerRegistry.GetAllToolsAsync()`.
- `SentinelCore.Host` — Host application wiring (DI, startup sequence, UI hooks).
