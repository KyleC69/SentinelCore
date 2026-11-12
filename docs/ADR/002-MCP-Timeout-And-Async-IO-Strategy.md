## Architecture Decision Record: MCP Timeout and Async File I/O Strategy (ADR-002)

**Status:** Proposed
**Date:** 2026-11-01
**Architects:** [Agent/Copilot]
**Impacted Components:** `Orchestrations/Mcp` (`McpConnectionFactory`, `McpServerRegistry`, `McpToolTimeoutFunction`, `JsonFileMcpServerRegistryStore`), `Orchestrations/Workflows/TheCoreWorkflow`, `SentinelCore.Tests/`

---

### 1. Context and Problem Statement

The application intermittently never completes (about 2 clean end-to-end runs out of hundreds). A paused debug session showed no
user code running: the WPF dispatcher was idle, and two thread-pool workers were parked in native reads on non-async handles
(`RandomAccess.ReadAtOffsetAsync`, 1024-byte buffer, offset -1, i.e. pipe-style stream reads) that stayed unchanged across breaks.
That is the signature of waiting on a stdio MCP server child process that stopped answering.
The MCP code had no upper bound on connect, tool listing, or tool invocation, so one unresponsive server blocks the agent run forever.

### 2. Decision

1. **Every MCP wait is bounded.**
   - Initialize handshake: `McpClientOptions.InitializationTimeout` = 30 s (SDK-native).
   - `McpServerRegistry.StartAsync` (connect + first tool listing): 60 s linked `CancellationTokenSource`.
   - `McpServerRegistry.GetAllToolsAsync` per server: 30 s; a timed-out server is logged and skipped.
2. **Tool calls are bounded by `McpToolTimeoutFunction`**, a sealed subclass of the framework type `DelegatingAIFunction`
   (Microsoft.Extensions.AI.Abstractions). Each tool returned by the registry is wrapped (default 30 s).
   A timeout or any server/transport exception is returned as `ToolResult.Fail(...)` and success as `ToolResult.Ok(...)`
   (server result serialized to JSON), honoring the rule that every tool returns `ToolResult` and never throws.
   Caller cancellation still propagates as `OperationCanceledException` because it is not a tool failure.
3. **Timeouts use linked `CancellationTokenSource.CancelAfter`**, distinguishing timeout from caller cancellation by
   `!cancellationToken.IsCancellationRequested`. No new orchestration layer or coordinator is introduced.
4. **Failed start must not leak a child process:** if tool listing fails after a connection was created, the client is disposed.
5. **File I/O rules**
   - No `.Wait()`, `.Result`, or `GetAwaiter().GetResult()` on async work inside async call paths
	 (`TheCoreWorkflow.BuildWorkflow` now awaits the debug visualization and tolerates IO failure).
   - Async file reads use `FileOptions.Asynchronous` (`JsonFileMcpServerRegistryStore.LoadAsync`).
   - Persisted state is written atomically (temp file + `File.Move(overwrite: true)`) and saves are serialized with a `SemaphoreSlim`.

### 3. Alternatives Considered

* Timeout only at the HTTP/stdio transport: the SDK exposes no per-call timeout, so tool calls would remain unbounded.
* Per-call `CancellationTokenSource` in each caller: duplicates logic (violates DRY); a single wrapper keeps the invariant in one place.

### 4. Consequences

* Slow but legitimate MCP tools longer than 30 s will now fail; the default is configurable per wrapper instance
  (future work: surface as configuration).
* The tool type exposed by the registry is `McpToolTimeoutFunction` rather than `McpClientTool`; no code depended on the concrete type.
* Remaining known, accepted risk: `SafetyRuleEngine.Evaluate` blocks with `Task.Wait(30 s)` (bounded, synchronous API by contract);
  small synchronous local-file reads/writes in `FileModelConfigStore`, `DpapiTokenCache` and the logging sinks are left unchanged.

### 5. Verification Strategy

fast call returns `ToolResult.Ok`, hung call and throwing tool return `ToolResult.Fail`, caller cancel is not a timeout, invalid timeout rejected.
2. Build of `SentinelCore.Orchestrations` with zero errors.
3. Manual: reproduce with a stdio MCP server that never responds; the run must continue with a failed tool call within ~30 s.
