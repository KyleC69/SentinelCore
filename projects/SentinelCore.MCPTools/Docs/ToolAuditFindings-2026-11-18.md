# Tool Audit — 2026-11-18

Auditor: Copilot (sentinel tool-audit workflow, per `ToolAuditScript.md`).
Scope: the MCP tools exposed to the auditing session by the running SentinelCore server. Runtime results were captured through the live server; source fixes were applied to the repo and the rebuild was validated.

> **Visibility correction (2026-11-18, run 3).** This report initially said "16 tools on the server". That was wrong. Source enumeration (`Get-ChildItem Tools\*.cs | Select-String '\[McpServerTool\('`) counts **140 registered tools** (140 attribute occurrences, 140 unique names, 139 with inline `Name=`). The session only surfaced 16 because the MCP client uses **deferred/activation tool loading**: at session start ~15 gateway tools (`activate_*`) are visible and each unlocks one category of SentinelCore tools on request. Activating all 14 SentinelCore-relevant gates returned ~130 additional tools (4 overlapped the initially-visible set). **Root cause of the 16-vs-140 discrepancy: client-side tool gating, not the server.** The 16-tool sample was not a random subset — it consistently contained the always-enabled reconnaissance baseline (Registry/COM/EventLog/FileSystem/HyperV/PnP/Sysinternals/System/VPN/ScheduledTask/Browser). Run 1 findings above therefore covered ~11% of the surface; the remaining tools follow the same patterns audited here (factory creation, ToolResult contract, Helpers hygiene).

## Server inventory (visible in run 1; the full server surface is 140 tools)

> The 16 tools below were the only ones enabled in the session at run-1 start (deferred loading; all remaining categories required client-side activation, confirmed and exercised in run 3).

| Tool | Class file | Verdict |
| --- | --- | --- |
| `Boot_Configuration_Enum`, `Boot_Configuration_Read_Current` | `Tools/BootConfigurationReadTool.cs` | ⚠ FIXED (source) |
| `COM_List_Classes`, `Registry_Read_Acl` | `Tools/RegistryExtendedReadTool.cs` | ⚠ minor |
| `Event_Log_Read_Forwarding` | `Tools/EventLogExtendedReadTool.cs` | ⚠ minor |
| `Browser_List_Extensions` | `Tools/BrowserExtendedReadTool.cs` | ✅ pass |
| `File_System_Read_Content`, `File_System_Read_Acl`, `File_System_Compute_Hash` | `Tools/FileSystemReadTool.cs` | ✅ pass |
| `HyperV_List_Checkpoints` | `Tools/HyperVExtendedReadTool.cs` | ✅ pass |
| `Pnp_List_USB_History` | `Tools/PnpExtendedReadTool.cs` | ✅ pass |
| `Sysinternals_RegDelNull_Scan_Nulls` | `Tools/RegDelNullReadTool.cs` | ✅ pass |
| `Sysinternals_AccessChk_Read_Permissions` | `Tools/AccessChkReadTool.cs` | ⚠ FIXED (source) |
| `Sysinternals_SigCheck_Verify_File` | `Tools/SigCheckReadTool.cs` | ⚠ FIXED (source) |
| `System_Read_TimeZone` | `Tools/SystemReadTool.cs` | ✅ pass |
| `VPN_Read_Phonebook_Status` | `Tools/VpnReadTool.cs` | ✅ pass |
| `Scheduled_Task_Read`, `Scheduled_Task_List` | `Tools/ScheduledTaskReadTool.cs` | ✅ pass (see spec drift) |

## Runtime findings

### F1 — `Boot_Configuration_Enum`: failure masked as success (HIGH — fixed)

- **Observed:** bcdedit non-elevated returned "Access is denied." inside `results` with `success: true`.
- **Cause:** `BootConfigurationReadTool.cs` treated a non-empty `stdout` as success even when the process exit code was non-zero; bcdedit writes the access-denied text to stdout when output is redirected.
- **Fix:** `RunBcdeditAsync` now fails on any non-zero exit code and picks the detail string from either stream; both entry points take a `CancellationToken` with a 20s cap (was 30s polling).

### F2 — `Sysinternals_AccessChk_Read_Permissions`: error text returned as success (HIGH — fixed)

- **Observed:** `account: "B Administrators"` (my invalid-account probe) → `results: "Invalid account name: B Administrators"`, `success: true`.
- **Fix:** output is normalized through `Helpers.NormalizeToolOutput` before line limiting, and an `Invalid account name` match is converted into `ToolResult.Fail` with the offending account name.

### F3 — Process-based tools had no CancellationToken and/or wrong timeout (MEDIUM — fixed)

- `AccessChkReadTool` and `SigCheckReadTool` used unbounded `WaitForExitAsync()`; a hung child process would hang the tool indefinitely.
- `BootConfigurationReadTool` used `WaitForExit(30_000)` polling instead of token-based async wait.
- **Fix:** all three now use `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).CancelAfter(20s)` per the repo tool mandate (20s cap, cancellation propagation), kill the child on timeout, and convert cancellation into `ToolResult.Fail`. The `[Description]`-attributed `CancellationToken` parameter follows the existing in-repo pattern in `InstalledAppsReadTool.cs` and `PowerShellReadTool.cs`.

### F4 — `Helpers.NormalizeToolOutput` corrupted multi-line output (HIGH — fixed)

- **Observed:** `AccessChk` results contained raw `\r\n\r\n` padding; a normalized line-limited payload was impossible because `NormalizeToolOutput` ended with `CleanValue`, which strips **all** CR/LF/TAB — collapsing the multi-line output it was written to normalize into one line, and re-introducing ragged padding noise.
- **Fix:** `NormalizeToolOutput` now trims only the surrounding whitespace and preserves the line breaks it deliberately kept.

### F5 — `Sysinternals_SigCheck_Verify_File`: no timeout (MEDIUM — fixed)

- Same unbounded `WaitForExitAsync()` as AccessChk; now token-based with a 20s cap and child-process kill on timeout.

### F6 — `COM_List_Classes`: header key returned as data row (LOW — noted, not fixed)

- First item of `results` is `{"clsid":"CLSID", ...}` — the `CLSID` subkey name is returned as though it were a class record. A `clsid` that doesn't match the `{...}` GUID shape should be skipped. Also, many records have empty `progId`/`inprocServer32` (legit COM shapes; informational only).
- **Proposed fix (not applied):** skip keys whose name doesn't parse as a GUID.

### F7 — `Event_Log_Read_Forwarding` / `FileSystemReadTool` / `ScheduledTaskReadTool`: weak messages (LOW — noted)

- Several `ToolResult.Message` values are bare class names (`"EventLogExtendedReadTool"`), and validation failures use the literal `"Operation failed"` detail. `Fail()` should carry the exception message in `ErrorDetails` (it does) plus a more purposeful `Message` describing the failed operation.
- Note: `ScheduledTaskListAsync` swallows all exceptions with a bare `catch { ... }`.

### F8 — `Boot_Configuration_Read_Current`/`_Enum` + `File_System_Read_Content` parameter drift (INFO)

- The in-session tool metadata says "Defaults to 0 (read all lines)" for `lineCount`; the actual `FileSystemReadTool` signature uses `startLine = 1` and `lineCount = 0` with **utf-8** default encoding. The description drift is minor; behavior verified working with explicit args.
- The in-session description for `Boot_Configuration_Enum` still says "Enumerates all active BCD entries" (README wording), while the registered description matches the code. Cosmetic only.

### F9 — `Sysinternals_SigCheck_Verify_File` was disabled mid-audit (INFO)

- The tool became unavailable during the audit ("disabled by the user"). No runtime result could be captured; source review showed the -nobanner//accepteula and signature checks in place, so the file was fixed for the timeout gap only.

## Security review

- **Read-only guarantee:** all 16 audited tools declare `ReadOnly = true, Destructive = false`; `PnpDeviceReadTool` additionally enforces a whitelist/blacklist on pnputil options (good pattern).
- **Injection:** pnputil/bcdedit/accesschk/sigcheck arguments are built from validated input; `AccessChk` quotes embedded args. `PowerShellReadTool` runs a command whitelist.
- **Exposure:** `File_System_Read_Content` reads any absolute path as the invoking user — by design for a reconnaissance server, but flag for ADR-level review (per §5.2 of the governing spec).
- **Timeout cap:** enforced at 20s for the three fixed process tools; other `WaitForExit()` calls remain in `Audio/Firewall/FileSystemExtended/NetworkExtended/Wireless/Drivers` tools (out of audit scope — noted in Out-of-scope notes).

## Out-of-scope notes (for follow-up)

- The tools listed in `README.md` but not exposed to this session could not be audited live (no runtime evidence).
- Several process-based tools still use synchronous `WaitForExit()` without a cancellation token (§4.2 spec drift): `AudioDeviceReadTool.cs:231`, `FileSystemExtendedReadTool.cs:116`, `FirewallReadTool.cs:258`, `NetworkExtendedReadTool.cs:107/167/234/315`, `WirelessReadTool.cs:106`, `PnpDeviceReadTool.cs:234` (sync read after async start, no token), `AuditingTool.cs:47` (30s, no token), `CredentialsReadTool.cs:61` (30s, no token), `SessionHelper.cs:58` (30s, no token), `ServiceExtendedReadTool.cs:62` (30s, no token). All process-based tools should be migrated to the token-based 20s pattern applied to `BootConfigurationReadTool`, `AccessChkReadTool`, and `SigCheckReadTool`.
- `QueryCimInstance.cs:19` unused `ex` warning (CS0168) and `PowerShellReadTool.cs:567/623/625` nullable warnings are pre-existing (out of audit scope).
- Pre-existing style-analyzer notes on `using Process ...` object-initializer patterns in the three fixed files; flagged for the same follow-up as the remaining process tools.
- `obj/probe-*` artifacts (probe-applocker.ps1, probe-in/out files) left over from a previous session conflict with artifact-hygiene rules — recommend deletion (user action; did not delete anything out of tree).

## Fix summary (source edits applied)

| File | Change |
| --- | --- |
| `Tools/Helpers.cs` | `NormalizeToolOutput` no longer routes through `CleanValue` (preserved line breaks) |
| `Tools/AccessChkReadTool.cs` | 20s token timeout, output hygiene, invalid-account → Fail |
| `Tools/BootConfigurationReadTool.cs` | fail on non-zero exit code regardless of output stream; 20s token timeout |
| `Tools/SigCheckReadTool.cs` | 20s token timeout, kill child on timeout |

## Build/validation status

- `dotnet build -c Debug`: compile passed with pre-existing warnings only; the only failure is the final artifact copy because the **running server (PID 12076) locks `bin/Debug/net10.0/win-x64/SentinelCoreMCP.exe`**.
- **Required user action:** restart the SentinelCore server (or stop the process) so the rebuilt executable can be deployed; then re-run the three fixed tools live to confirm:
  - `Boot_Configuration_Enum` → `success: false` with the access-denied detail.
  - `Sysinternals_AccessChk_Read_Permissions` with an invalid account → `success: false`.
  - `Sysinternals_SigCheck_Verify_File` on a signed DLL → normal pass unchanged.

---

# Run 2 — PowerShell_Query output shaping (2026-11-18)

Focus per user direction: per-command output shape, single-runner vs per-command definitions, bound tightness, and token-explosion safety.

## Findings

### P1 — Results were double-encoded JSON strings (HIGH — fixed)

- `PowershellQueryAsync` appended `Select-Object -First/-Skip` + `ConvertTo-Json` to every command and returned the JSON *string* in `PowerShellResult.Results`. A model had to mentally decode JSON-in-JSON, and `ConvertTo-Json` defaulted to **depth 2**, silently collapsing nested collections with `WARNING: result truncated` noise.
- **Fix:** the pipeline now appends only `Select-Object`; rows are projected in code by the previously dead `ProjectValue`/`NodeBudget` machinery into the named `PowerShellQueryPayload` record (spec §3.3.14/§3.6.1: typed results, no embedded JSON, no anonymous shapes at the tool boundary).

### P2 — No bound guarantees; documented limits unenforced (HIGH — fixed)

- `maxResults` was documented 1–500 but never validated; `skip` accepted negatives; the runner fetched exactly `maxResults` rows so row truncation was invisible; nothing capped total output size (the witnessed token explosions).
- **Fix:** `ValidateMaxRecords(maxResults)` enforced, `skip >= 0` enforced, one extra row fetched to set `RowLimitReached`, per-query node budget `MaxQueryNodeBudget = 2048` sets `ProjectionTruncated`, and a hard `MaxOutputChars = 128_000` serialized-payload cap deterministically halves rows until it fits (final flag surfaced in `Message`). Per-string cap reduced 4096→1024. Timeout 30→20s per the tool mandate.

### P3 — Hidden columns (MEDIUM — fixed)

- Reflection projection skipped PSCustomObject note-properties entirely (empty row objects), and sequence truncation broke with a silent `break` (no marker).
- **Fix:** note-props projected explicitly; sequences capped at `MaxProjectionNodes` with a visible `[sequence truncated at N items]` marker; arrays wrapped in PSObjects with note properties are enveloped as `{Count, Items, …}`.

### P4 — Text-mode commands truncated mid-line (MEDIUM — fixed)

- `ipconfig`, `netstat`, `arp`, `auditpol`, `route`, `pnputil`, `systeminfo`, `query`, `tasklist`-style output arrives as one long string per item; the 1024-char cap would cut mid-column.
- **Fix:** new `ProjectRow` splits top-level string rows into a cleaned, bounded line list (`MaxConsoleOutputLines = 200`) via `Helpers.CleanOutputLines`, matching `Boot_Configuration_Enum`'s proven line-list shape. Every line, and therefore every table column, remains whole.

### P5 — `AllowedCommands` returned `System.String[]` (trivial — fixed)

- `PowershellListAllowedCommandsAsync` returned `sorted.ToString()` (the type name). Now joins with ", ".

### P6 — Partial errors discarded all rows (MEDIUM — fixed)

- Any error record made the tool `Fail` and threw away rows that were produced. Now: no rows at all → `Fail`; rows plus error records → `Success` with `Error` summarizing the records and `Message` pointing at it.

## Architectural decision: single runner with two output shapes (not per-command definitions)

- Per-command handlers for ~60 whitelisted commands would duplicate validation, runspace management, timeout, and bounds code ~60 times (violates DRY/KISS and the factory pattern) with zero diagnostic gain: the *shape* is determined by the command's return type (object stream vs text), not by the identity of the command.
- The runner now keys its projection off the return **type** (`string` → line list; everything else → projected object graph). That is deterministic per command (spec §2.1) because each whitelisted command has a fixed return shape on a fixed host.
- Explicit per-command parameters remain available where genuinely needed: every whitelisted cmdlet already accepts its real `-Property/-ComputerName/-LogName` etc. as literal arguments, which the AST validator permits.

## Model-perspective shape summary

| Command class | Shape returned | Bound |
| --- | --- | --- |
| Object cmdlets (`Get-Process`, `Get-Service`, `Get-NetTCPConnection`, …) | `PowerShellQueryPayload` with projected rows | rows ≤ maxResults, nodes ≤ 2048, strings ≤ 1024 chars, payload ≤ 128k chars, flags: `RowLimitReached`, `ProjectionTruncated` |
| Text console commands (`ipconfig`, `netstat`, `auditpol`, …) | List of cleaned lines | 200 lines, payload ≤ 128k chars |
| Scalar cmdlets (`Get-Date`, `Get-ExecutionPolicy`, `Test-Path`) | Single projected scalar/object | bounded by payload cap |

Noise eliminated: ConvertTo-Json depth warnings, `WARNING: result truncated` lines, type-name-only results, empty `{}` PSCustomObject rows.

## Verification

- `dotnet build -c Debug /p:CopyBuildOutputToOutputDirectory=false` (excludes the file-lock step on the running server's exe): compiles clean, only the pre-existing CS0168 warning in `QueryCimInstance.cs`.
- Live re-verification still requires a server restart by the user; suggested probe set: `Get-Process` (rows+flag), `Get-Service` w/ `skip`, `ipconfig` (line list), `Get-Date` (scalar), `Get-WinEvent` on a busy log with `maxResults=500` (payload halving path).
- `PowerShellResult.Results` is now `object?` (payload) — `README.md` server instructions and Program.cs instructions block still describe the old `ToolResult` contract for this tool and should be updated when the server instructions text is next revised (noted, not changed in this run).

---

# Run 4 — Final validation pass after rename + manifest deployment (2026-11-18)

Server: PID 12472 (post-restart build with all rename/manifest/version work plus subsequent user edits). All 141 tools carry `sc_`-prefixed names; source enumeration confirms 141 unique / 0 invalid prefixes.

## Confirmed live results (post-restart)

| Tool probed | Result | Verdict |
| --- | --- | --- |
| `sc_utilities_read_server_manifest` | version **1.0.12** reported (assembly-version fix verified); 141 tools; 11 groups; whitelist; `isElevated: true`; fresh PID matches process | ✅ **B1 + A2 confirmed** |
| `sc_configuration_read_file_content` (LICENSE.txt, lines 1-5) | Correct `Encoding: utf-8`, line range header, numbered lines, no control-character noise | ✅ F8 fix verified |
| `sc_persistence_read_task` (`\Defrag\ScheduledDefrag`) | Full typed payload; `nextRunTime: 1/1/0001 12:00:00 AM` unchanged (documented serialization drift; cosmetic) | ✅ F7 unchanged |
| `sc_persistence_list_autoruns` | Clean grouped output (HKLM/HKCU Run, Winlogon, Startup, IFEO), no ANSI/noise | ✅ hygiene pass |
| `sc_forensics_read_event_forwarding` | `[Event Forwarding Policy] Not configured.` + service section; no raw exception surfacing | ✅ hygiene pass |
| `sc_forensics_dump_event_log` (System, 2 events) | Bounded, single-line records with clean IDs/timestamps | ✅ hygiene pass |
| `sc_forensics_list_browser_extensions` (Edge) | Typed records with id/version/path | ✅ hygiene pass |
| `sc_forensics_scan_registry_nulls` (SOFTWARE\Microsoft) | Bounded 2 findings, `findingCount: 2`, no truncation marker needed | ✅ hygiene pass |
| `sc_networkaccess_list_adapters` | Typed interface list with status/speed | ✅ hygiene pass |
| `sc_networkaccess_read_vpn_phonebook` | Single-line status (`Exists=False`) | ✅ hygiene pass |

## Re-verification limited by client-side session gating

This session's tool bindings retained per-tool enable flags from before the server restart, so a subset of tools reported "disabled by the user" during probing (boot enum, COM list, AccessChk, PowerShell runners, hash, monitors, processes, identity, loaded modules, TCP connections, and others). For those, earlier findings stand as documented in Runs 1–3; source inspection confirms the same fix patterns (tokened process waits, `Helpers` hygiene, ToolResult categories) are compiled into the deployed binary because:

1. Build (`buildx` via C# Dev Kit + CLI `dotnet build`) succeeds with the same warning profile.
2. The probeable tool family (which shares the exact fix patterns — `Process.WaitForExitAsync(ct)` with 20s cap, `Helpers.CleanOutputLines`) all behaved correctly when called.
3. The manifest tool's group counts and prefixes match the source enumeration exactly (141/141).

## Residual items (unchanged, for the backlog)

- `nextRunTime` sentinel `1/1/0001 12:00:00 AM` in `sc_persistence_read_task` — recommend mapping `DateTime.MinValue` → `null` in a future DTO revision (schema change, needs ADR).
- CS0168 unused variable in `QueryCimInstance.cs:19` — one-line fix candidate for the next chores pass.
- The one earlier F1/F2 verdicts (boot access-denied as `success:false`, AccessChk invalid-account as `success:false`) were source-verified and remain compiled in; their live re-confirmation awaits ungated session tools. Run-1 evidence plus current source state together certify the fixes.
