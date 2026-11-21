# SentinelCore-MCP

![SentinelCore-MCP project banner displaying the project name SentinelMCP in large lettering, representing a Windows security and configuration reconnaissance server built on the Model Context Protocol. The clean, professional design sets a technical tone for the documentation that follows.](SentinelCore-MCP/assets/sentinelmcp.png)

A Windows security and configuration reconnaissance server implementing the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/). SentinelCore-MCP exposes **141 read-only tools** that allow AI assistants and MCP clients to inspect Windows system state — firewall rules, Defender status, registry keys, services, processes, certificates, and much more — without modifying anything on the host.

This server is a tool module for the Sentinel Core Forensic Investigation Platform. The tools are universal and could be used elsewhere if appropriate.
All tools are read-only and are designed only for Windows 10-11 Operating Systems. Server implements the MCP protocol over the stdio transport, and is discoverable by MCP clients.
Server can run alongside Sentinel Core or be placed on a separate host for remote inspection of Windows systems. Ideal for enterprise environments where AI agents need to inspect Windows hosts without installing additional software or agents.

> **Windows only.** This server uses Windows-specific APIs (WMI, CIM, Registry, COM, Sysinternal tools) and is marked `<UnsupportedOSPlatform>linux;osx</UnsupportedOSPlatform>`.

---

## Features

- **141 read-only tools** across 11 named tool groups covering Windows security, configuration, and diagnostics (names follow `sc_<groupprefix>_<verb>_<noun>`; retrieve the live catalogue with `sc_utilities_read_server_manifest`)
- **Never throws** — every tool returns a `ToolResult` object with `Success`, `Results`, `Message`, and, on failure, `ErrorDetails` plus `ErrorCategory`/`ErrorCode`
- **All tools are marked `ReadOnly = true, Destructive = false`** — safe for AI agents to call
- **`server/discover` support** — responds to MCP discovery requests with server capabilities, instructions, and supported protocol versions (2026-07-28 revision, SEP-2575)
- **Stdio transport** — integrates with VS Code, Visual Studio, Claude Desktop, and any MCP-compatible client
- **Self-contained single-file executable** — no .NET runtime required on the target machine
- **NuGet package** — install via `dnx` or `dotnet tool install`

---

## Tool Catalog
## Tool Catalog

All 141 tools are read-only and named `sc_<groupprefix>_<verb>_<noun>` per [ADR-006](`Docs/ToolingComponent.md`). Call `sc_utilities_read_server_manifest` for the live, exact catalogue; it is generated from the registered tools, so it is always current.

| `sc_configuration_list_directory` | Lists a directory |
| `sc_configuration_list_env_variables` | Lists environment variables |
| `sc_configuration_list_installed_apps` | Lists installed applications |
| `sc_configuration_list_msi_products` | Lists MSI products |
| `sc_configuration_list_notification_apps` | Lists notification-enabled apps |
| `sc_configuration_list_registry_keys` | Lists registry subkeys under a key |
| `sc_configuration_list_search_scopes` | Lists search indexing scopes |
| `sc_configuration_list_updates` | Lists installed hotfixes (patch history) |
| `sc_configuration_read_boot_current` | Returns the current boot entry GUID |
| `sc_configuration_read_boot_enum` | Enumerates active BCD entries |
| `sc_configuration_read_chrome_policies` | Reads Chrome policy entries |
| `sc_configuration_read_default_browser` | Reads the default browser ProgId |
| `sc_configuration_read_env_path` | Analyzes system and user PATH for hijack risk |
| `sc_configuration_read_env_variable` | Reads an environment variable |
| `sc_configuration_read_file_acl` | Reads the ACL of a file or directory |
| `sc_configuration_read_file_content` | Reads a text file (bounded line range, binary rejected) |
| `sc_configuration_read_file_metadata` | Reads metadata and attributes of a path |
| `sc_configuration_read_ie_settings` | Reads IE zone and security settings |
| `sc_configuration_read_notification_settings` | Reads notification quiet hours |
| `sc_configuration_read_os_info` | Reads OS version and hotfixes via Sysinternals PsInfo |
| `sc_configuration_read_registry_acl` | Reads the ACL of a registry key |
| `sc_configuration_read_registry_value` | Reads a registry value |
| `sc_configuration_read_search_settings` | Reads search indexing settings |
| `sc_configuration_read_system_info` | Reads OS version, build, uptime, hostname |
| `sc_configuration_read_time_zone` | Reads the system time zone |
| `sc_configuration_read_update_settings` | Reads Windows Update policy settings |

### Hardening — `sc_hardening_` (Policy, audit, firewall, Defender, BitLocker, security posture)

| Tool | Description |
| ---- | ----------- |
| `sc_hardening_list_applocker_rules` | Lists AppLocker rule collections |
| `sc_hardening_list_bitlocker_volumes` | Lists BitLocker-protected volumes |
| `sc_hardening_list_firewall_rules` | Lists firewall rules |
| `sc_hardening_list_group_policy` | Lists local group policy values under a policy root |
| `sc_hardening_read_applocker_policy` | Retrieves the effective AppLocker policy as XML |
| `sc_hardening_read_audit_policy` | Reads the advanced auditing policy (auditpol) |
| `sc_hardening_read_bitlocker_volume` | Reads BitLocker volume metadata and key protectors |
| `sc_hardening_read_credential_guard` | Reads Credential Guard status |
| `sc_hardening_read_defender_config` | Reads Defender exclusions and registry config |
| `sc_hardening_read_defender_status` | Reads Defender AV status via WMI |
| `sc_hardening_read_exploit_protection` | Reads Exploit Protection settings (DEP/ASLR/CFG) |
| `sc_hardening_read_firewall_profiles` | Reads firewall profile settings |
| `sc_hardening_read_group_policy` | Reads a local group policy registry value |
| `sc_hardening_read_rsop` | Reads the Resultant Set of Policy (RSOP) |
| `sc_hardening_read_secureboot` | Reads Secure Boot status |
| `sc_hardening_read_smartscreen` | Reads SmartScreen settings |
| `sc_hardening_read_token_elevation` | Reports whether the process token is elevated |
| `sc_hardening_read_tpm` | Reads TPM status |
| `sc_hardening_read_uac_policy` | Reads UAC policy settings |

### Forensics — `sc_forensics_` (Artefact and timeline examination)

| Tool | Description |
| ---- | ----------- |
| `sc_forensics_dump_event_log` | Dumps an event log via Sysinternals PsLogList |
| `sc_forensics_list_browser_extensions` | Lists installed Chrome and Edge extensions |
| `sc_forensics_list_com_classes` | Lists COM class registrations for hijacking detection |
| `sc_forensics_list_dcom_apps` | Lists registered DCOM application IDs |
| `sc_forensics_list_pending_renames` | Lists file operations scheduled for the next reboot |
| `sc_forensics_list_pipes` | Lists named pipes for covert-channel detection |
| `sc_forensics_list_taskbar_pinned` | Lists taskbar pinned items |
| `sc_forensics_read_dcom_settings` | Reads DCOM application settings |
| `sc_forensics_read_ntfs_volume` | Reads NTFS volume geometry via Sysinternals NTFSInfo |
| `sc_forensics_read_permissions` | Audits effective permissions via Sysinternals AccessChk |
| `sc_forensics_read_shell_settings` | Reads Explorer policy and desktop settings |
| `sc_forensics_scan_registry_nulls` | Scans a registry subtree for embedded-null values (RegDelNull detection) |

### Forensics (continued) — event logs

| Tool | Description |
| ---- | ----------- |
| `sc_forensics_list_event_channels` | Lists event log channels and their configuration |
| `sc_forensics_list_file_streams` | Lists NTFS alternate data streams on a path |
| `sc_forensics_query_event_log` | Queries events from a channel by level and time range |
| `sc_forensics_read_event_config` | Reads event log channel retention and size configuration |
| `sc_forensics_read_event_forwarding` | Reads Windows Event Forwarding subscription configuration |
| `sc_forensics_read_hosts_file` | Reads the hosts file for DNS redirection detection |

### Identity — `sc_identity_` (Accounts, groups, tokens, privileges)

| Tool | Description |
| ---- | ----------- |
| `sc_identity_browse_ad_container` | Browses an Active Directory container (requires domain join) |
| `sc_identity_check_domain_availability` | Checks whether the host is joined to a domain |
| `sc_identity_list_credentials` | Lists stored credential targets (no passwords) |
| `sc_identity_list_groups` | Lists local groups |
| `sc_identity_list_logon_sessions` | Lists active logon sessions via Sysinternals LogonSessions |
| `sc_identity_list_rdp_sessions` | Lists listening RDP sessions |
| `sc_identity_list_sessions` | Lists active logon sessions |
| `sc_identity_list_users` | Lists local user accounts |

### NetworkAccess — `sc_networkaccess_` (Connections, listeners, shares, remote access)

| Tool | Description |
| ---- | ----------- |
| `sc_networkaccess_list_adapters` | Lists network adapters |
| `sc_networkaccess_list_endpoints` | Lists TCP endpoints via Sysinternals TcpView |
| `sc_networkaccess_list_listening_ports` | Lists TCP and UDP listening ports with process association |
| `sc_networkaccess_list_shares` | Lists network shares |
| `sc_networkaccess_list_tcp_connections` | Lists active TCP connections |
| `sc_networkaccess_list_vpn_connections` | Lists configured VPN/RAS connections |
| `sc_networkaccess_list_wireless_history` | Lists wireless connection history for rogue AP detection |
| `sc_networkaccess_list_wireless_interfaces` | Lists wireless interfaces |
| `sc_networkaccess_list_wireless_profiles` | Lists wireless profiles |
| `sc_networkaccess_read_arp_table` | Reads the ARP cache table |
| `sc_networkaccess_read_dns_cache` | Reads the DNS resolver cache |
| `sc_networkaccess_read_dns_settings` | Reads DNS server settings |
| `sc_networkaccess_read_ip_config` | Reads IP configuration for adapters |
| `sc_networkaccess_read_rdp_listener` | Reads the RDP listener configuration |
| `sc_networkaccess_read_rdp_settings` | Reads RDP settings |
| `sc_networkaccess_read_routing_table` | Reads the IPv4 routing table |
| `sc_networkaccess_read_system_proxy` | Reads system proxy configuration |
| `sc_networkaccess_read_vpn_phonebook` | Reads the RAS phonebook directory status |
| `sc_networkaccess_read_winhttp_proxy` | Reads WinHTTP proxy configuration |
| `sc_networkaccess_resolve_hostname` | Resolves a hostname to addresses |

### Cryptography — `sc_cryptography_` (Certificates, keys, hashing)

| Tool | Description |
| ---- | ----------- |
| `sc_cryptography_compute_file_hash` | Computes a file hash (MD5 through SHA512) |
| `sc_cryptography_list_certificates` | Lists certificates in a store |
| `sc_cryptography_read_certificate` | Reads certificate details by thumbprint |
| `sc_cryptography_verify_certificate` | Verifies a certificate trust chain and revocation |
| `sc_cryptography_verify_file_signature` | Verifies Authenticode signature via Sysinternals SigCheck |

### Execution — `sc_execution_` (Process and command execution surface, read-only)

| Tool | Description |
| ---- | ----------- |
| `sc_execution_list_processes` | Lists running processes via CIM |
| `sc_execution_list_processes_pslist` | Lists process CPU/memory statistics via Sysinternals PsList |
| `sc_execution_read_process` | Reads details for a process by PID |

### MemoryInspection — `sc_memoryinspection_` (Process memory and module inspection)

| Tool | Description |
| ---- | ----------- |
| `sc_memoryinspection_capture_process_dump` | Captures a full process dump via Sysinternals ProcDump (writes a .dmp file; requires elevation) |
| `sc_memoryinspection_list_handles` | Lists open handles via Sysinternals Handle |
| `sc_memoryinspection_list_loaded_modules` | Lists DLLs loaded by a process via Sysinternals ListDLLs |

### Persistence — `sc_persistence_` (Scheduled tasks, services, autostart)

| Tool | Description |
| ---- | ----------- |
| `sc_persistence_list_autoruns` | Lists autostart locations |
| `sc_persistence_list_ifeo` | Lists Image File Execution Options debuggers |
| `sc_persistence_list_service_details` | Reads the ACL of a Windows service |
| `sc_persistence_list_services` | Lists Windows services |
| `sc_persistence_list_services_psservice` | Lists services via Sysinternals PsService |
| `sc_persistence_list_tasks` | Lists scheduled tasks in a folder |
| `sc_persistence_read_service` | Reads a service configuration |
| `sc_persistence_read_task` | Reads a scheduled task definition |

### Hardware — `sc_hardware_` (Devices, drivers, firmware, storage, peripherals)

| Tool | Description |
| ---- | ----------- |
| `sc_hardware_list_audio_devices` | Lists active audio devices |
| `sc_hardware_list_batteries` | Lists battery status |
| `sc_hardware_list_drivers` | Lists installed kernel drivers |
| `sc_hardware_list_hyperv_checkpoints` | Lists Hyper-V checkpoints for rollback detection |
| `sc_hardware_list_hyperv_switches` | Lists Hyper-V virtual switches |
| `sc_hardware_list_hyperv_vms` | Lists Hyper-V virtual machines |
| `sc_hardware_list_monitors` | Lists active display monitors |
| `sc_hardware_list_perf_categories` | Lists performance counter categories |
| `sc_hardware_list_perf_counters` | Lists counters in a category |
| `sc_hardware_list_pnp_devices` | Lists PnP devices |
| `sc_hardware_list_power_plans` | Lists power plans |
| `sc_hardware_list_power_settings` | Lists active power plan settings |
| `sc_hardware_list_printer_jobs` | Lists print jobs for a queue |
| `sc_hardware_list_printers` | Lists printer queues |
| `sc_hardware_list_sensors` | Lists sensors |
| `sc_hardware_list_usb_history` | Lists USB device connection history (USBSTOR) for removable-media forensics |
| `sc_hardware_list_video_controllers` | Lists display adapters and current video mode |
| `sc_hardware_read_battery_settings` | Reads battery-related power settings |
| `sc_hardware_read_cpu_topology` | Reads CPU topology and virtualization via Sysinternals Coreinfo |
| `sc_hardware_read_default_audio_device` | Reads the default audio device |
| `sc_hardware_read_display_geometry` | Reads virtual screen geometry |
| `sc_hardware_read_hyperv_vm` | Reads Hyper-V VM settings |
| `sc_hardware_read_location_sensor` | Reads the location sensor state |
| `sc_hardware_read_perf_counter` | Reads a performance counter value |
| `sc_hardware_read_pnp_device` | Reads PnP device and property details |
| `sc_hardware_read_printer` | Reads a printer queue |

### Utilities — `sc_utilities_` (Query runners and server self-inspection)

| Tool | Description |
| ---- | ----------- |
| `sc_utilities_list_powershell_commands` | Lists the allowed PowerShell commands |
| `sc_utilities_list_wmi_classes` | Lists available WMI classes |
| `sc_utilities_query_powershell` | Executes a whitelisted read-only PowerShell command in a constrained runspace (bounded, projected results) |
| `sc_utilities_query_wmi` | Executes a read-only WQL SELECT against WMI/CIM |
| `sc_utilities_read_server_manifest` | Reads the server manifest: tools by group, whitelist, version, PID, elevation state |

---

## Getting Started

### Prerequisites

- Windows 10/11 or Windows Server 2019+
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source)

### Running from Source

```bash
dotnet run --project SentinelCore-MCP
```

### Configuring in VS Code

Add to your `.vscode/mcp.json` or VS Code settings:

```json
{
  "servers": {
    "SentinelCore-MCP": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH-TO-SENTINELCORE-MCP>"]
    }
  }
}
```

### Configuring in Visual Studio

Create a `.mcp.json` file in your solution directory:

```json
{
  "servers": {
    "SentinelCore-MCP": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH-TO-SENTINELCORE-MCP>"]
    }
  }
}
```

### Installing as a NuGet Tool

```bash
dotnet tool install --global SentinelCore-MCP
```

Then configure your MCP client:

```json
{
  "servers": {
    "SentinelCore-MCP": {
      "type": "stdio",
      "command": "dnx",
      "args": ["SentinelCore-MCP", "--version", "0.1.0-beta", "--yes"]
    }
  }
}
```

---

## Architecture

### ToolResult Contract

Every tool returns a `ToolResult` object — **never throws**:

```csharp
public class ToolResult
{
    public bool    Success      { get; set; }
    public string? Results      { get; set; }
    public string? ErrorDetails { get; set; }
    public string? Message      { get; set; }

    public static ToolResult Ok(string results, string message = "Ok");
    public static ToolResult Fail(string errorDetails, string message = "Fail");
}
```

- **Success path** → `ToolResult.Ok(jsonOrText)`
- **Error path** → `ToolResult.Fail("descriptive error: {ex.Message}")`
- All `catch` blocks include the exception message for diagnostics

### Design Principles

1. **Read-only by default** — every `[McpServerTool]` is marked `ReadOnly = true, Destructive = false`
2. **Never throw** — exceptions are caught and wrapped in `ToolResult.Fail(...)`
3. **No custom types** — tools use built-in .NET and Windows API types exclusively
4. **Stdio transport** — communicates over stdin/stdout using the MCP protocol
5. **Discoverable** — supports `server/discover` (2026-07-28 revision) for capability probing without initialization handshake

---

## Publishing

1. Update `<PackageId>` and `<PackageVersion>` in `SentinelCore-MCP.csproj`
2. Run `dotnet pack -c Release`
3. Publish to NuGet.org:

```bash
dotnet nuget push bin/Release/*.nupkg --api-key <YOUR-API-KEY> --source https://api.nuget.org/v3/index.json
```

---

## Links

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [MCP C# SDK](https://modelcontextprotocol.github.io/csharp-sdk)
- [ModelContextProtocol NuGet](https://www.nuget.org/packages/ModelContextProtocol)
- [Use MCP Servers in VS Code](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP Servers in Visual Studio](https://learn.microsoft.com/visualstudio/ide/mcp-servers)

## License

This project is licensed under the terms found in the [LICENSE](../LICENSE) file.
