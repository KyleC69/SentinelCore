---
title: "Tooling Component"
status: Active
component: Tooling
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Tooling Component

**Project:** `SentinelCore.Orchestrations`
**Namespace:** `SentinelCore.Orchestrations.Application`
**Dependencies:** `SentinelCore.Contracts` (AgentRole, AITool), `Microsoft.Extensions.AI`
**Consumers:** `SentinelCore.Orchestrations` (AgentSpecBuilder, DomainAgentFactory, CoreAgentFactory, MagneticCoopOrchestration)

---

## Purpose

The Tooling component provides **centralized, static tool resolution** for all agents in SentinelCore. It defines 30+ domain-specific read tools mapped to Windows configuration surfaces, exposes role-based toolsets, and serves as the single source of truth for "which tools does this agent get?"

**Key Principle:** Tools are **read-only** — they gather evidence, they don't mutate system state. Mutation is handled by the host application, not agents.

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  Tooling Component                         │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │           ToolRegistry (static class)               │  │  │
│  │  │  • DomainToolNames: Dictionary<string, string[]>   │  │  │
│  │  │  • RoleToolNames: Dictionary<AgentRole, string[]>  │  │  │
│  │  │  • GetToolByDomain(domain) → IList<AITool>?        │  │  │
│  │  │  • GetToolsByNames(names) → IReadOnlyList<AITool>  │  │  │
│  │  │  • GetToolsetByRole(role) → IReadOnlyList<AITool>  │  │  │
│  │  │  • GetAllTools() → IReadOnlyList<AITool>           │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ consumed by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  AgentSpecBuilder.GetToolsetByRole(role)                        │
│  DomainAgentFactory.CreateAgent(domain, description)            │
│  CoreAgentFactory.Create()  (adds MCP tools on top)             │
│  MagneticCoopOrchestration (direct role-based toolsets)         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. DomainToolNames — 30 Windows Configuration Domains

**File:** `Application/ToolRegistry.cs`
**Type:** `static readonly Dictionary<string, string[]>`

Maps **domain name** → **array of tool names** (each tool name maps to a function in `AIFunctionFactory`).

```csharp
private static readonly Dictionary<string, string[]> DomainToolNames = new(StringComparer.OrdinalIgnoreCase)
{
    ["registry"] = ["read_registry_key", "search_registry_keys", "read_registry_value"],
    ["filesystem"] = ["read_file", "list_directory", "search_files", "get_file_hash"],
    ["environment"] = ["get_environment_variables", "get_environment_variable"],
    ["bootconfig"] = ["read_bcd_store", "get_boot_configuration"],
    ["accessibility"] = ["get_accessibility_settings", "get_high_contrast_settings", "get_narrator_settings"],
    ["searchindexing"] = ["get_search_index_status", "get_indexed_locations", "get_search_indexer_performance"],
    ["shellexplorer"] = ["get_shell_folder_settings", "get_file_associations", "get_context_menu_handlers"],
    ["certificates"] = ["list_certificates", "get_certificate_details", "get_certificate_chain"],
    ["eventlog"] = ["query_event_log", "get_event_log_names", "export_event_log"],
    ["applocker"] = ["get_applocker_policy", "get_applocker_rules", "get_applocker_events"],
    ["windowsupdate"] = ["get_windows_update_status", "get_update_history", "get_pending_updates"],
    ["pnpdevices"] = ["list_pnp_devices", "get_device_properties", "get_device_driver_details"],
    ["hyperv"] = ["list_virtual_machines", "get_vm_configuration", "get_vm_network_adapters"],
    ["audio"] = ["get_audio_devices", "get_audio_endpoint_properties", "get_audio_session_info"],
    ["printers"] = ["list_printers", "get_printer_properties", "get_print_queue"],
    ["grouppolicy"] = ["get_gpo_list", "get_gpo_details", "get_rsop_data"],
    ["firewall"] = ["get_firewall_rules", "get_firewall_profiles", "get_firewall_logs"],
    ["localaccounts"] = ["list_local_users", "get_user_properties", "list_local_groups"],
    ["rdp"] = ["get_rdp_configuration", "get_rdp_sessions", "get_rdp_security_settings"],
    ["services"] = ["list_services", "get_service_details", "get_service_dependencies"],
    ["scheduledtasks"] = ["list_scheduled_tasks", "get_task_details", "get_task_history"],
    ["power"] = ["get_power_plan", "get_power_settings", "get_battery_status"],
    ["network"] = ["get_network_adapters", "get_network_connections", "get_dns_client_settings"],
    ["dcom"] = ["get_dcom_configuration", "get_dcom_applications", "get_dcom_security"],
    ["wmi"] = ["query_wmi", "get_wmi_namespaces", "get_wmi_class_definition"],
    ["drivers"] = ["list_drivers", "get_driver_details", "get_driver_signature_status"],
    ["processes"] = ["list_processes", "get_process_details", "get_process_modules"],
    ["performance"] = ["get_performance_counters", "get_system_performance", "get_process_performance"],
    ["installedapps"] = ["list_installed_applications", "get_application_details", "get_application_uninstall_info"],
    ["browserconfig"] = ["get_browser_policies", "get_browser_extensions", "get_browser_profiles"],
    ["fonts"] = ["list_installed_fonts", "get_font_details", "get_font_substitutions"],
    ["notifications"] = ["get_notification_settings", "get_notification_history", "get_focus_assist_settings"],
    ["vpn"] = ["list_vpn_connections", "get_vpn_connection_details", "get_vpn_profile_xml"],
    ["wireless"] = ["get_wireless_profiles", "get_wireless_networks", "get_wireless_adapter_settings"],
    ["proxy"] = ["get_proxy_configuration", "get_winhttp_proxy", "get_ie_proxy_settings"],
    ["sensors"] = ["list_sensors", "get_sensor_data", "get_sensor_properties"],
    ["battery"] = ["get_battery_status", "get_battery_health", "get_battery_charge_history"],
    ["display"] = ["get_display_configuration", "get_monitor_details", "get_display_scaling"],
    ["credentials"] = ["list_credential_manager_entries", "get_credential_details", "list_web_credentials"],
    ["uac"] = ["get_uac_configuration", "get_uac_prompt_behavior", "get_consent_prompt_behavior"],
    ["defender"] = ["get_defender_status", "get_defender_scan_history", "get_defender_threat_history"],
    ["bitlocker"] = ["get_bitlocker_status", "get_bitlocker_volume_status", "get_bitlocker_key_protectors"]
};
```

### Domain Categories

| Category | Domains |
|----------|---------|
| **Core OS** | registry, filesystem, environment, bootconfig |
| **User Experience** | accessibility, searchindexing, shellexplorer, notifications, display, fonts |
| **Security** | certificates, applocker, firewall, localaccounts, rdp, credentials, uac, defender, bitlocker |
| **System Management** | windowsupdate, pnpdevices, services, scheduledtasks, power, drivers, processes, performance |
| **Networking** | network, dcom, wmi, vpn, wireless, proxy |
| **Virtualization** | hyperv |
| **Hardware** | audio, printers, sensors, battery |
| **Applications** | installedapps, browserconfig |
| **Policy & Config** | grouppolicy, dcom, wmi |

---

## 2. RoleToolNames — Role-Based Toolsets

**File:** `Application/ToolRegistry.cs`
**Type:** `static readonly Dictionary<AgentRole, string[]>`

Maps **AgentRole** → **tool names** (referencing `DomainToolNames` keys or direct tool names).

```csharp
private static readonly Dictionary<AgentRole, string[]> RoleToolNames = new()
{
    [AgentRole.Core] = [
        "read_registry_key", "search_registry_keys", "read_registry_value",
        "read_file", "list_directory", "search_files", "get_file_hash",
        "get_environment_variables", "get_environment_variable",
        "query_event_log", "get_event_log_names", "export_event_log",
        "list_services", "get_service_details", "get_service_dependencies",
        "list_processes", "get_process_details", "get_process_modules",
        "list_pnp_devices", "get_device_properties", "get_device_driver_details",
        "get_windows_update_status", "get_update_history", "get_pending_updates",
        "get_firewall_rules", "get_firewall_profiles", "get_firewall_logs",
        "get_gpo_list", "get_gpo_details", "get_rsop_data",
        "get_defender_status", "get_defender_scan_history", "get_defender_threat_history",
        "get_bitlocker_status", "get_bitlocker_volume_status", "get_bitlocker_key_protectors"
    ],

    [AgentRole.Manager] = [],  // No tools — orchestrates only

    [AgentRole.Domain] = [],   // Resolved dynamically via GetToolByDomain

    [AgentRole.Worker] = [
        "read_file", "list_directory", "search_files",
        "query_wmi", "get_wmi_namespaces", "get_wmi_class_definition",
        "list_processes", "get_process_details"
    ],

    [AgentRole.General] = [
        "read_file", "list_directory", "search_files"
    ],

    [AgentRole.Aggregator] = []  // No tools — aggregates only
};
```

### Role Toolset Summary

| Role | Tool Count | Purpose |
|------|------------|---------|
| `Core` | ~40 | Full forensic investigation toolkit |
| `Manager` | 0 | Pure orchestration |
| `Domain` | Dynamic (3 typical) | Per-domain via `GetToolByDomain` |
| `Worker` | 7 | General investigation + WMI + processes |
| `General` | 3 | Basic file/evidence gathering |
| `Aggregator` | 0 | Result synthesis only |

---

## 3. Public API

### GetToolByDomain(string domain)

```csharp
public static IList<AITool>? GetToolByDomain(string domain)
{
    if (!DomainToolNames.TryGetValue(domain, out var toolNames))
        return null;

    return toolNames.Select(name => AIFunctionFactory.CreateFunctionByName(name))
                    .Where(tool => tool != null)
                    .Cast<AITool>()
                    .ToList();
}
```

**Returns:** `IList<AITool>?` — null if domain not found, empty list if tools not registered in factory.

**Used by:** `DomainAgentFactory.CreateAgent(domain, description)`

---

### GetToolsByNames(IEnumerable<string> toolNames)

```csharp
public static IReadOnlyList<AITool> GetToolsByNames(IEnumerable<string> toolNames)
{
    return toolNames.Select(name => AIFunctionFactory.CreateFunctionByName(name))
                    .Where(tool => tool != null)
                    .Cast<AITool>()
                    .ToList();
}
```

**Returns:** `IReadOnlyList<AITool>` — filters out nulls (unregistered tools).

**Used by:** `GetToolsetByRole`, `AgentSpecBuilder`

---

### GetToolsetByRole(AgentRole role)

```csharp
public static IReadOnlyList<AITool> GetToolsetByRole(AgentRole role)
{
    if (!RoleToolNames.TryGetValue(role, out var toolNames))
        return [];

    return GetToolsByNames(toolNames);
}
```

**Returns:** `IReadOnlyList<AITool>` — empty list if role not found.

**Used by:** `AgentSpecBuilder.BuildAgentSpec(role)` → `AgentSpec.Tools`

---

### GetAllTools()

```csharp
public static IReadOnlyList<AITool> GetAllTools()
{
    return DomainToolNames.Values
        .SelectMany(names => names)
        .Distinct()
        .Select(name => AIFunctionFactory.CreateFunctionByName(name))
        .Where(tool => tool != null)
        .Cast<AITool>()
        .ToList();
}
```

**Returns:** `IReadOnlyList<AITool>` — all registered tools across all domains.

**Used by:** Diagnostics, tool discovery, testing.

---

## 4. AIFunctionFactory Integration

**External Dependency:** `Microsoft.Extensions.AI` / `AIFunctionFactory`

Tools are **not defined in ToolRegistry** — they are created by name via `AIFunctionFactory.CreateFunctionByName(name)`. The factory must be configured with all tool implementations at startup.

### Expected Tool Signatures (by name pattern)

| Tool Name Pattern | Expected Signature |
|-------------------|-------------------|
| `read_*` | `string ReadX(string path/key/id)` |
| `list_*` | `IEnumerable<X> ListX(string filter?)` |
| `search_*` | `IEnumerable<X> SearchX(string query)` |
| `get_*` | `X GetX(string id)` |
| `query_*` | `IEnumerable<X> QueryX(string query)` |
| `export_*` | `byte[] ExportX(string path)` |

### Registration Pattern (in DI setup)

```csharp
services.AddSingleton<IAIFunctionFactory, AIFunctionFactory>();

// Each tool registered as a function:
// builder.Services.AddAIFunction("read_registry_key", () => new ReadRegistryKeyFunction(...));
// builder.Services.AddAIFunction("search_registry_keys", () => new SearchRegistryKeysFunction(...));
// ... etc for all 30+ tools
```

---

## 5. Consumer Integration

### AgentSpecBuilder (Role → Tools)

```csharp
// In AgentSpecBuilder.BuildAgentSpec(AgentRole role)
IReadOnlyList<AITool> tools = ToolRegistry.GetToolsetByRole(role);
// → AgentSpec.Tools = tools
```

### DomainAgentFactory (Domain → Tools)

```csharp
// In DomainAgentFactory.CreateAgent(string domain, string description)
IList<AITool>? domainTools = ToolRegistry.GetToolByDomain(domain);
IReadOnlyList<AITool> tools = domainTools is not null ? domainTools.ToList() : [];
// → AgentSpec.Tools = tools (overrides role defaults)
```

### CoreAgentFactory (Core + MCP)

```csharp
// In CoreAgentFactory.Create()
AgentSpec baseSpec = _specBuilder.BuildAgentSpec(AgentRole.Core);
// baseSpec.Tools already has Core toolset from ToolRegistry

List<AITool> coreTools = [
    new MicrosoftDocsSearchTool(mcp),
    new MicrosoftDocsFetchTool(mcp),
    new MicrosoftCodeSampleSearchTool(mcp)
];

AgentSpec spec = baseSpec with { Tools = [.. baseSpec.Tools, .. coreTools] };
```

### MagneticCoopOrchestration (Direct Role Toolsets)

```csharp
var manager = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Manager));
// Manager gets RoleToolNames[Manager] = []

var worker1 = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.Worker));
// Worker gets RoleToolNames[Worker] = 7 tools

var critic = _agentBuilder.Build(_specBuilder.BuildAgentSpec(AgentRole.General));
// Critic gets RoleToolNames[General] = 3 tools
```

---

## 6. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Static tool registry | ✅ | `ToolRegistry` is static class |
| Domain → tools mapping | ✅ | `DomainToolNames` dictionary |
| Role → tools mapping | ✅ | `RoleToolNames` dictionary |
| Tools are read-only | ✅ | All tools are evidence-gathering |
| Core gets MCP at factory | ✅ | Not in registry |
| Domain tools dynamic | ✅ | `GetToolByDomain` |
| No circular dependencies | ✅ | Depends only on Contracts + Microsoft.Extensions.AI |

---

## 7. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| `AIFunctionFactory` registration | DI setup | ⚠️ Not in this component |
| Tool implementations | Separate project | ❌ Not in Orchestrations |
| Tool validation at startup | `ToolRegistry` | ❌ No validation |
| Domain tool count consistency | `DomainToolNames` | ⚠️ Most have 3, some vary |
| Role toolset for Aggregator | `RoleToolNames` | ✅ Empty (by design) |
| Dynamic composite agents | `MagneticCoopOrchestration` | ⚠️ References 'dynamic_agent' tool |
| Tool versioning | Registry | ❌ Not implemented |

---

## 8. Related Documentation

| Document | Description |
|----------|-------------|
| `DynamicAgentsComponent.md` | AgentSpecBuilder, AgentSpec, role-based construction |
| `DomainAgentSurfaces.md` | DomainAgentFactory, CoreAgentFactory, tool integration |
| `OrchestrationComponent.md` | MagneticCoopOrchestration, agent construction pipeline |
| `ContractsComponent.md` | `AgentRole`, `AITool`, `ModelSettings` definitions |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
