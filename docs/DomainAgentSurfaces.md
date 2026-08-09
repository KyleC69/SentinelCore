---
title: "Domain Agent Surfaces Component"
status: Active
component: DomainAgentSurfaces
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Domain Agent Surfaces Component

**Project:** `SentinelCore.Orchestrations`
**Namespaces:** `SentinelCore.Orchestrations.Application`, `SentinelCore.Orchestrations.Agents.Domain`, `SentinelCore.Orchestrations.Tools`
**Dependencies:** `SentinelCore.Contracts` (abstractions, settings, events), `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`
**Consumers:** `SentinelCore.Orchestrations` (orchestrators), `SentinelCoreHost` (via DI)

---

## Purpose

The Domain Agent Surfaces component defines **how domain-specific agents are configured, what tools they have access to, and how they are instantiated**. It bridges the gap between the generic agent construction pipeline (`IAgentSpecBuilder` → `IAgentBuilder`) and the concrete domain-specific capabilities needed for Windows system investigation.

This component consists of:
1. **ToolRegistry** — Static registry mapping 30+ Windows domains to read-only tools
2. **DomainAgentFactory** — Factory that creates domain agents with domain-specific toolsets
3. **Domain Tool Implementations** — 30+ read-only tools for Windows system surfaces
4. **MCP Research Tools** — Microsoft Docs search/fetch/code-sample tools for Core agent

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Agent Construction Pipeline             │  │
│  │  IAgentSpecBuilder → AgentSpec → IAgentBuilder → AIAgent  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              ▲                                   │
│                              │ uses                              │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  Domain Agent Surfaces                     │  │
│  │  ┌─────────────────┐  ┌─────────────────────────────────┐  │  │
│  │  │  ToolRegistry   │  │      DomainAgentFactory         │  │  │
│  │  │  (30+ domains)  │──▶│  CreateAgent(domain, desc)      │  │  │
│  │  │  Role toolsets  │  │  Overrides: name, tools, desc   │  │  │
│  │  └─────────────────┘  └─────────────────────────────────┘  │  │
│  │  ┌─────────────────────────────────────────────────────┐   │  │
│  │  │           CoreAgentFactory (MCP tools)              │   │  │
│  │  └─────────────────────────────────────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ depends on
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  AgentRole, AgentSpec, IAgentBuilder, IAgentSpecBuilder,        │
│  ISentinelCoreEvents, SentinelCoreSettings, ModelSettings       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. ToolRegistry — Static Tool Resolution

**File:** `Application/ToolRegistry.cs`
**Pattern:** Static class with pre-initialized dictionaries — **no DI registration, no lifecycle management**

### Design Philosophy

> "Tool registry is not a registry in the traditional sense, there are no DI registrations, no service lifetimes, and no dependency injection of tools to be used by domain agents. They are to be instantiated by the domain agent and used as needed. The registry is a collection of toolsets, a domain agent is assigned a subset of which are available to the domain agents. This registry is used to retrieve tools by the target domain they interact with. The registry is not responsible for the lifecycle of the tools, it is only responsible for providing access to the tools."

Tools are **instantiated on-demand** by the factory when creating agents. The registry only provides **name-to-type mapping**.

### Domain → Tool Mapping (`DomainToolNames`)

```csharp
private static readonly Dictionary<string, IReadOnlyList<string>> DomainToolNames =
    new(StringComparer.OrdinalIgnoreCase)
{
    ["registry"] = [nameof(RegistryReadTool)],
    ["dcom"] = [nameof(DcomReadTool)],
    ["wmi"] = [nameof(WmiQueryTool)],
    ["filesystem"] = [nameof(FileSystemReadTool)],
    ["grouppolicy"] = [nameof(GroupPolicyReadTool)],
    ["services"] = [nameof(WindowsServiceReadTool)],
    ["scheduledtasks"] = [nameof(ScheduledTaskReadTool)],
    ["network"] = [nameof(NetworkReadTool)],
    ["firewall"] = [nameof(FirewallReadTool)],
    ["power"] = [nameof(PowerSettingsReadTool)],
    ["localaccounts"] = [nameof(LocalAccountsReadTool)],
    ["eventlog"] = [nameof(EventLogReadTool)],
    ["applocker"] = [nameof(AppLockerReadTool)],
    ["windowsupdate"] = [nameof(WindowsUpdateReadTool)],
    ["pnpdevices"] = [nameof(PnpDeviceReadTool)],
    ["environment"] = [nameof(EnvironmentVariablesReadTool)],
    ["shellexplorer"] = [nameof(ShellExplorerReadTool)],
    ["certificates"] = [nameof(CertificateStoreReadTool)],
    ["hyperv"] = [nameof(HyperVReadTool)],
    ["rdp"] = [nameof(RemoteDesktopReadTool)],
    ["bootconfig"] = [nameof(BootConfigurationReadTool)],
    ["accessibility"] = [nameof(AccessibilityReadTool)],
    ["searchindexing"] = [nameof(SearchIndexingReadTool)],
    ["audio"] = [nameof(AudioDeviceReadTool)],
    ["printers"] = [nameof(PrinterReadTool)],
    ["drivers"] = [nameof(DriversReadTool)],
    ["processes"] = [nameof(ProcessesReadTool)],
    ["performance"] = [nameof(PerformanceReadTool)],
    ["installedapps"] = [nameof(InstalledAppsReadTool)],
    ["browserconfig"] = [nameof(BrowserConfigReadTool)],
};
```

**30 Domains** — All read-only, Windows system inspection tools.

### Role → Toolset Mapping (`RoleToolNames`)

```csharp
private static readonly Dictionary<AgentRole, IReadOnlyList<string>> RoleToolNames =
    new()
{
    [AgentRole.Core] = [],           // Core gets MCP tools at factory level
    [AgentRole.Manager] = [],        // Manager has NO tools
    [AgentRole.Domain] = [],         // Domain tools resolved by domain name at factory
    [AgentRole.Worker] = [           // General worker toolset
        nameof(FileSystemReadTool),
        nameof(RegistryReadTool),
        nameof(ProcessesReadTool),
        nameof(EventLogReadTool),
        nameof(NetworkReadTool)
    ],
    [AgentRole.General] = [          // General purpose toolset
        nameof(FileSystemReadTool),
        nameof(RegistryReadTool),
        nameof(ProcessesReadTool)
    ],
    [AgentRole.Aggregator] = []      // Aggregator has NO tools
};
```

### Public API

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetToolByDomain` | `IList<AITool>? GetToolByDomain(string domain)` | Instantiates all tools for a domain |
| `GetToolsByNames` | `IList<AITool> GetToolsByNames(IEnumerable<string> toolNames)` | Instantiates tools by exact type names |
| `GetToolsetByRole` | `IList<AITool> GetToolsetByRole(AgentRole role)` | Gets role-based default toolset |
| `GetAllTools` | `IList<AITool> GetAllTools()` | Instantiates ALL registered tools |
| `HasDomain` | `bool HasDomain(string domain)` | Checks if domain exists |
| `HasTool` | `bool HasTool(string toolName)` | Checks if tool name exists |
| `GetDomainNames` | `IReadOnlyList<string> GetDomainNames()` | Lists all domain names |
| `GetToolNames` | `IReadOnlyList<string> GetToolNames()` | Lists all tool type names |

### Tool Instantiation Pattern

```csharp
// Internal: uses Activator.CreateInstance for each tool name
private static AITool? CreateTool(string toolName)
{
    Type? toolType = Assembly.GetExecutingAssembly()
        .GetTypes()
        .FirstOrDefault(t => t.Name == toolName && typeof(AITool).IsAssignableFrom(t));

    return toolType != null ? (AITool?)Activator.CreateInstance(toolType) : null;
}
```

**All tools must:**
- Be in `SentinelCore.Orchestrations.Tools` namespace
- Inherit from `Microsoft.Extensions.AI.AITool`
- Have a parameterless constructor
- Be read-only (no mutating operations)

---

## 2. DomainAgentFactory — Domain Agent Creation

**File:** `Agents/Domain/DomainAgentFactory.cs`
**Interface:** None (concrete class, registered as transient in DI)

### Constructor

```csharp
public DomainAgentFactory(IAgentSpecBuilder specBuilder, IAgentBuilder agentBuilder)
```

### `CreateAgent(string domain, string description)`

```csharp
public AIAgent CreateAgent(string domain, string description)
{
    // 1. Validate domain
    ArgumentException.ThrowIfNullOrWhiteSpace(domain);

    // 2. Get base spec from AgentSpecBuilder (role-based defaults)
    AgentSpec baseSpec = _specBuilder.BuildAgentSpec(AgentRole.Domain);

    // 3. Resolve domain-specific tools
    IList<AITool>? domainTools = ToolRegistry.GetToolByDomain(domain);
    IReadOnlyList<AITool> tools = domainTools is not null ? domainTools.ToList() : [];

    // 4. Override spec with domain-specific values
    AgentSpec spec = baseSpec with
    {
        AgentName = $"{domain}_agent",           // e.g., "registry_agent"
        Tools = tools,                            // Domain-specific toolset
        Persona = string.IsNullOrWhiteSpace(description)
            ? baseSpec.Persona
            : baseSpec.Persona with { Description = description }  // Override description
    };

    // 5. Build via shared pipeline (logging, events, middleware)
    return _agentBuilder.Build(spec);
}
```

### Key Behaviors

| Aspect | Behavior |
|--------|----------|
| **Agent Name** | `{domain}_agent` (e.g., `registry_agent`, `services_agent`) |
| **Tools** | Exactly the tools mapped to `domain` in `DomainToolNames` |
| **Persona** | Base `Domain` persona from `PersonaFactory`, description overridden if provided |
| **Model** | `DomainModel` from `SentinelCoreSettings` (via `AgentSpecBuilder`) |
| **Role** | `AgentRole.Domain` (determines event routing: `MagneticParticipantActivity`) |
| **Lifetime** | Per-task (created fresh for each investigation step) |

### Domain Agent Persona

From `PersonaFactory.PersonaRegistry[PersonaType.DomainAgent]`:

```csharp
Name = "Domain Specialist"
Description = "A specialized agent with deep expertise in a specific Windows system domain..."
Instructions = "You are a domain specialist agent. Your role is to investigate and analyze..."
```

**Overridden at factory:** `Description` replaced with caller-provided description (e.g., "Investigate Windows Registry configuration for startup items")

---

## 3. Domain Tool Implementations

**Namespace:** `SentinelCore.Orchestrations.Tools`
**Base:** `Microsoft.Extensions.AI.AITool`
**Pattern:** All tools are **read-only** — they query Windows system state, never modify it.

### Tool Categories (30 domains)

| Domain | Tool Class | Purpose |
|--------|------------|---------|
| `registry` | `RegistryReadTool` | Read registry keys/values |
| `dcom` | `DcomReadTool` | DCOM configuration |
| `wmi` | `WmiQueryTool` | WMI/CIM queries |
| `filesystem` | `FileSystemReadTool` | File/directory enumeration |
| `grouppolicy` | `GroupPolicyReadTool` | Group Policy objects |
| `services` | `WindowsServiceReadTool` | Windows services |
| `scheduledtasks` | `ScheduledTaskReadTool` | Task Scheduler tasks |
| `network` | `NetworkReadTool` | Network adapters, connections |
| `firewall` | `FirewallReadTool` | Windows Firewall rules |
| `power` | `PowerSettingsReadTool` | Power plans, settings |
| `localaccounts` | `LocalAccountsReadTool` | Local user accounts |
| `eventlog` | `EventLogReadTool` | Event Log entries |
| `applocker` | `AppLockerReadTool` | AppLocker policies |
| `windowsupdate` | `WindowsUpdateReadTool` | Windows Update status |
| `pnpdevices` | `PnpDeviceReadTool` | Plug-and-play devices |
| `environment` | `EnvironmentVariablesReadTool` | Environment variables |
| `shellexplorer` | `ShellExplorerReadTool` | Shell namespace items |
| `certificates` | `CertificateStoreReadTool` | Certificate stores |
| `hyperv` | `HyperVReadTool` | Hyper-V VMs/switches |
| `rdp` | `RemoteDesktopReadTool` | RDP configuration |
| `bootconfig` | `BootConfigurationReadTool` | Boot configuration (BCD) |
| `accessibility` | `AccessibilityReadTool` | Accessibility settings |
| `searchindexing` | `SearchIndexingReadTool` | Windows Search index |
| `audio` | `AudioDeviceReadTool` | Audio devices |
| `printers` | `PrinterReadTool` | Printer queues |
| `drivers` | `DriversReadTool` | Installed drivers |
| `processes` | `ProcessesReadTool` | Running processes |
| `performance` | `PerformanceReadTool` | Performance counters |
| `installedapps` | `InstalledAppsReadTool` | Installed applications |
| `browserconfig` | `BrowserConfigReadTool` | Browser policies/config |

### Tool Implementation Pattern

```csharp
public sealed class RegistryReadTool : AITool
{
    public RegistryReadTool() : base("registry_read", "Read Windows Registry keys and values")
    {
        Parameters = JsonSchema.FromType<RegistryReadParams>();
    }

    protected override async Task<object> InvokeAsync(
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        // Parse arguments
        var params = JsonSerializer.Deserialize<RegistryReadParams>(arguments);

        // Execute read-only Windows API call
        // Return structured result
    }
}
```

**All tools follow this pattern:** parameter schema → read-only execution → structured JSON result.

---

## 4. CoreAgentFactory — MCP Research Tools

**File:** `Agents/Core/CoreAgentFactory.cs`
**Interface:** `ICoreAgentFactory` (registered as singleton)

### Purpose

Creates **The Core Agent** — the central investigative agent with:
- Role-based defaults from `AgentSpecBuilder` (`AgentRole.Core`)
- **MCP (Model Context Protocol) research tools** requiring runtime endpoint

### MCP Tools Added

```csharp
const string mcp = "https://learn.microsoft.com/api/mcp";

List<AITool> coreTools = [
    new MicrosoftDocsSearchTool(mcp),      // Search Microsoft Docs
    new MicrosoftDocsFetchTool(mcp),       // Fetch specific doc pages
    new MicrosoftCodeSampleSearchTool(mcp) // Search code samples
];
```

### Core Agent Spec Merge

```csharp
AgentSpec spec = baseSpec with
{
    Tools = [.. baseSpec.Tools, .. coreTools]  // Append MCP tools to role defaults
};
```

### Planned Case Manipulation Tools (TODO)

```csharp
// CASE MANIPULATION TOOLS FOR THE CORE AGENT (planned):
//   case_append_signals(caseid)
//   case_append_resolution(caseid)
//   case_append_evidence(caseid)
//   query_pattern_memory()
//   case_escalate_touser()
//   case_request_user_clarification()
//   case_complete_case()
//   web_search_tool()
```

---

## 5. AgentRole → Tool/Event Routing Summary

| Role | Tools | Event Channel | Lifetime | Factory |
|------|-------|---------------|----------|---------|
| `Core` | Role defaults + MCP tools | `TheCoreActivity` / `TheCoreReasoning` / `TheCoreTooling` | Application | `CoreAgentFactory` |
| `Manager` | **None** | `MagneticParticipantActivity` | Workflow | `MagneticCoopOrchestration` |
| `Domain` | Domain-specific (30 domains) | `MagneticParticipantActivity` | Per-task | `DomainAgentFactory` |
| `Worker` | General toolset (5 tools) | `MagneticParticipantActivity` | Per-task | `AgentSpecBuilder` + `AgentBuilder` |
| `General` | Basic toolset (3 tools) | `MagneticParticipantActivity` | Per-task | `AgentSpecBuilder` + `AgentBuilder` |
| `Aggregator` | **None** | `MagneticParticipantActivity` | Per-task | `AgentSpecBuilder` + `AgentBuilder` |

---

## 6. Configuration Integration

### `SentinelCoreSettings` → Domain Agents

```csharp
public sealed class SentinelCoreSettings
{
    public ModelSettings? DomainModel { get; set; }  // Used by AgentRole.Domain
    // ...
}
```

**Flow:** `SentinelCoreSettings.DomainModel` → `AgentSpecBuilder.ResolveModel(AgentRole.Domain)` → `AgentSpec.Model` → `AgentBuilder` → `OllamaApiClient` → `AIAgent`

### SkillsDirectory (Deprecated)

```csharp
public string SkillsDirectory { get; set; } = string.Empty;  // Deprecated
```

**Note:** Marked deprecated in settings. Skills are now strongly-typed configuration classes, not file-based.

---

## 7. Integration with Orchestration

### MagneticCoopOrchestration Usage

```csharp
// In MagneticCoopOrchestration.BuildMagWorkflow():
var manager = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Manager));
var worker1 = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Worker));
var worker2 = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Worker));
var critic = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.General));
var aggregator = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Aggregator));
```

### TheCoreOrchestration Usage

```csharp
// In TheCoreOrchestration.InitiateAsync():
var coreAgent = _coreAgentFactory.Create();  // Core + MCP tools
var magneticResult = await _magneticOrchestration.ExecuteTasksAsync(plan, coreAgent);
```

---

## 8. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| No DI registration of tools | ✅ | Static registry, `Activator.CreateInstance` |
| Tools are read-only | ✅ | All 30 tools query-only |
| Domain agents per-task lifetime | ✅ | `DomainAgentFactory.CreateAgent()` called per task |
| Core agent application lifetime | ✅ | `ICoreAgentFactory` singleton |
| AgentRole determines event channel | ✅ | Via `EventPublishingChatClient` middleware |
| MCP tools only on Core agent | ✅ | Added in `CoreAgentFactory`, not in registry |
| Manager/Aggregator have no tools | ✅ | Enforced by `RoleToolNames` empty lists |

---

## 9. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| Case manipulation tools for Core agent | `CoreAgentFactory.cs` (commented) | ❌ Not implemented |
| `web_search_tool` for Core | `CoreAgentFactory.cs` (commented) | ❌ Not implemented |
| Tool parameter validation schemas | `Tools/*.cs` | ⚠️ Partial |
| Tool result standardization | `Tools/*.cs` | ⚠️ Inconsistent |
| Domain tool unit tests | `SentinelCore.Tests/` | ❌ Missing |
| Dynamic tool registration (plugin) | `ToolRegistry` | ❌ Not designed |

---

## 10. Related Documentation

| Document | Description |
|----------|-------------|
| `OrchestrationComponent.md` | Agent construction pipeline, orchestration strategies |
| `DynamicAgentsComponent.md` | Agent roles, specs, personas, dynamic creation |
| `ToolingComponent.md` | Complete tool registry API, all 30+ tools detail |
| `ContractsComponent.md` | `AgentRole`, `AgentSpec`, `SentinelCoreSettings`, events |
| `MemoryLayerComponent.md` | Pattern memory integration with Core agent |
| `SafetyRailsComponent.md` | Safety middleware, tool validation rules |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
