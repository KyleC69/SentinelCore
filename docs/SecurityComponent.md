---
title: "Message Provenance Security Component"
status: Active
component: SentinelCoreSecurity
last_updated: 2026-11-20
version: v1.0
---

# SentinelCore Message Provenance Security Component

This document is the reference for the message-provenance security module: how messages are signed, how signatures are validated before a model sees a message, how rejected messages are quarantined, and which parts are stubs or planned work.

**Project:** `SentinelCoreSecurity` — source lives in the sibling solution folder `SentinelCoreSecurity/` (`F:\Solutions\SentinelCoreSecurity`, outside the repository root) and is a member of `SentinelCore.slnx`.

**Root namespace:** `SentinelCore.Security`

**Project dependencies:** `SentinelCore.Contracts` plus NuGet `Microsoft.Extensions.AI.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions` only. The module never references Orchestrations, UI, CaseFlowEngine, or the hosts.

**Decision record:** [ADR-008](./ADR/008-Message-Provenance-Security-Module.md)

**Locked patterns:** PL-10 in [../architecture/pattern-lock.md](../architecture/pattern-lock.md)

Use this document when changing provenance stamping, validation points, quarantine behavior, or the identity registry, and when planning the SentinelCoreService input pathway.

## Implementation Status

The module began as an unscoped proof of concept and has been formalized. The current split:

| Capability | Status |
| --- | --- |
| HMAC-SHA256 provenance service (`SentinelProvenanceService`) | Implemented |
| Stamp on `ChatMessage.AdditionalProperties` (same mechanism as ChatType) | Implemented |
| Identity registry (in-memory stub, first-party seeds) | Implemented as stub; durable registry planned |
| Bounded in-memory quarantine store with event + warning logging | Implemented; durable persistence planned |
| Workflow entry gate (`ProvenanceGateExecutor`) with fail-closed dead end | Implemented |
| Model-boundary integrity re-check (`TheCoreSwitchExec`) | Implemented |
| Operator UI stamping (`CoreChatViewModel`) | Implemented |
| DI registration (`AddSentinelSecurity`, wired by `AddSentinelCore`) | Implemented |
| Registry management UI page | Planned |
| Quarantine browser UI page | Planned |
| Durable quarantine persistence | Planned |
| Durable identity registry | Planned |
| SentinelCoreService signed input pathway | Planned (identifier registered; transport not implemented) |

## Purpose and Threat Model

Sentinel Core accepts investigation input from the operator UI today and is designed to accept input from several external sources. The security module gives every message that enters the system through an approved pathway a verifiable origin, and keeps everything else out of the model pipeline.

Goals:

1. **Provenance** — every message entering through an approved entry point is signed with an HMAC that binds the registered sender identity, a timestamp, a per-message nonce, and the message contents.
2. **Pre-model validation at multiple points** — signatures are validated before a model ever sees a message: once at the workflow entry gate (nonce-consuming) and again at the model boundary (non-consuming integrity re-check).
3. **Fail-closed containment** — a message that arrives through an unapproved point, carries no stamp, carries an invalid stamp, or fails any validation check is quarantined exactly as it arrived and never reaches a model.

| Threat | Mitigation |
| --- | --- |
| Content injected through an unapproved entry point | Entry gate `Validate`; unsigned messages dead-end before `SafetyExecutor` |
| Tampering in transit or in-process | HMAC covers identifier + timestamp + nonce + role + text; `VerifyIntegrity` re-check at the model boundary catches post-gate tampering |
| Forged sender identity | The identifier must be registered, and the signature binds identifier to content with the shared key |
| Replay of a captured message | Per-message nonce consumed at entry validation (`ReplayDetected`) |
| Stale or future-dated capture | Stamp must fall inside ±`TimestampTolerance` (default 5 minutes) |
| Malicious content echoed into logs | Quarantine logging deliberately excludes message content |
| Key unavailable (misconfiguration or incident response) | Validation fails closed (`KeyUnavailable`) and the message is quarantined; stamping throws instead of producing unverifiable stamps |

## Module Layout

| Path | Role |
| --- | --- |
| `Provenance.cs` | `MessageProvenance` stamp record, `ProvenanceFailure` reasons, `ProvenanceValidationResult`, `WithProvenance`/`GetProvenance` extensions |
| `ISentinelProvenanceService.cs` | Signing/validation surface (`Stamp`, `Validate`, `VerifyIntegrity`) |
| `SentinelProvenanceService.cs` | HMAC-SHA256 implementation, canonical payload, nonce cache |
| `SentinelSecurityOptions.cs` | Module configuration (key variable name, tolerances, capacities) |
| `Cryptography/ISentinelKeyProvider.cs` | Process-boundary key abstraction (fail closed) |
| `Cryptography/EnvironmentSentinelKeyProvider.cs` | Reads the key from an environment variable per request |
| `Identity/ISentinelIdentityRegistry.cs` | Approved-sender registry abstraction |
| `Identity/RegisteredIdentity.cs`, `Identity/IdentityKind.cs` | Registry entries (`LocalOperator`, `DeployedService`, `ExternalSource`) |
| `Identity/InMemorySentinelIdentityRegistry.cs` | Thread-safe in-memory stub, seeds first-party identities |
| `Identity/SentinelIdentities.cs` | Canonical first-party identifiers (`CoreUi`, `CoreService`) |
| `Quarantine/IQuarantineStore.cs`, `Quarantine/QuarantineRecord.cs` | Quarantine abstraction and as-is record |
| `Quarantine/InMemoryQuarantineStore.cs` | Bounded in-memory store; logs via `ISystemReporter`; raises `RecordQuarantined` |
| `Infrastructure/DependencyInjection/SentinelSecurityServiceExtensions.cs` | `AddSentinelSecurity` DI registration |

## Provenance Stamp

The stamp is a `MessageProvenance` sealed record:

| Field | Meaning |
| --- | --- |
| `Identifier` | Registered identity that produced the message |
| `TimestampUtc` | When the message was stamped (UTC) |
| `Nonce` | Base64 of 16 random bytes (`RandomNumberGenerator`), consumed at entry validation |
| `Signature` | Base64 HMAC-SHA256 over the canonical payload |

It is stored on `ChatMessage.AdditionalProperties` under the key `"provenance"` through the `WithProvenance` / `GetProvenance` extensions — **the same stamping mechanism used for ChatType** (`WithChatType`). Stamping is in place: the extension returns the same instance. The metadata is local to the process and is not serialized to AI providers.

## Canonical Payload and Signature

```text
<identifier>\n<timestampUtc "O", invariant culture>\n<nonce>\n<chat role value>\n<message text>
```

- UTF-8 encoded; signed with HMAC-SHA256 using the UTF-8 bytes of the shared key.
- The signature is stored Base64 and compared with `CryptographicOperations.FixedTimeEquals` (constant time).
- The timestamp uses the round-trip `"O"` format with `InvariantCulture` so stamping and verification agree byte-for-byte.

## Validation Pipeline

Both APIs run the same ordered integrity checks; each failure short-circuits to a failed `ProvenanceValidationResult`:

| Order | Check | Failure |
| --- | --- | --- |
| 1 | Message is not null | `MissingProvenance` |
| 2 | A stamp is present | `MissingProvenance` |
| 3 | `Identifier`, `Nonce`, `Signature` are all non-empty | `MalformedProvenance` |
| 4 | Key is obtainable from `ISentinelKeyProvider` | `KeyUnavailable` |
| 5 | Identifier is registered | `UnknownIdentity` |
| 6 | Timestamp within ±`TimestampTolerance` | `ExpiredTimestamp` |
| 7 | HMAC matches recomputed signature | `InvalidSignature` |
| 8 | (`Validate` only) Nonce not previously consumed | `ReplayDetected` |

Two validation surfaces exist deliberately:

| API | Consumes the nonce | Intended use |
| --- | --- | --- |
| `Validate` | Yes | Outermost entry points; enables replay detection |
| `VerifyIntegrity` | No | Downstream re-checks; a message already validated at the entry point must not trip replay detection |

If validation itself throws, callers treat the message as `InternalError` and quarantine it — the module fails closed even against its own defects.

## Validation Points

| # | Point | Surface | API | Consumes nonce | Failure behavior |
| --- | --- | --- | --- | --- | --- |
| 1 | Operator UI entry | `CoreChatViewModel.SendAsync` | `Stamp(msg, SentinelIdentities.CoreUi)` | — | Stamp throws when the key is unavailable or the identity is unregistered; the send aborts |
| 2 | Workflow entry gate | `ProvenanceGateExecutor` (first node of `TheCoreWorkflow`) | `Validate` | Yes | Quarantine + user-visible rejection notice + `null` return; conditional edge filters `null`, so the message dead-ends |
| 3 | Model boundary re-check | `TheCoreSwitchExec`, before the routing decision and any agent contact | `VerifyIntegrity` | No | Quarantine + rejection notice + `null`; the outgoing `IntegrityGatePassed` edge filters `null`, so the message never reaches `ChatTypeRouterExecutor` |
| 4 | SentinelCoreService input pathway | Not implemented | `Validate` | Yes | Planned — see below |

## Workflow Integration (MAF Semantics)

- The gate is the first node: `TheCoreWorkflow` builds `new WorkflowBuilder(executors.ProvenanceGateExecutor)` and wires `AddEdge<ChatMessage>(executors.ProvenanceGateExecutor, executors.SafetyExecutor, ProvenanceGatePassed)` where the condition is `output is not null`.
- In MAF, the handler **return value** flows across conditional edges; `YieldOutputAsync` only feeds the output stream registered with `WithOutputFrom`. A rejected message yields a rejection notice for the user, returns `null`, matches no outgoing edge, and dead-ends.
- `ProvenanceGateExecutor` follows PL-9: `[YieldsOutput(typeof(ChatMessage))]`, sealed `partial` class, conventional constructor, `[MessageHandler] HandleChatMessageAsync` returning `ValueTask<ChatMessage?>`.
- The gate quarantines through `IQuarantineStore` and does **not** log the quarantine itself — the store is the single source of quarantine reporting.

## Quarantine Design

- `IQuarantineStore.Quarantine(message, reason, detail)` stores the message reference **exactly as it arrived** (no sanitization, no rebuild) and returns a `QuarantineRecord(QuarantinedAtUtc, Reason, Detail, Message)`.
- `InMemoryQuarantineStore` keeps a bounded `ConcurrentQueue` (`QuarantineCapacity`, default 500; oldest evicted).
- Every quarantine event is reported once through `ISystemReporter.ReportWarning` as `[SentinelSecurity] Message quarantined. Reason: <reason>. Detail: <detail>` — **message content is deliberately excluded** because quarantined content may be malicious.
- The store raises `RecordQuarantined` after each event so a future quarantine browser can observe quarantines live.
- Retention is in-memory only; records are lost on restart. Durable persistence is planned future work and should follow the factory-per-operation persistence rules (PL-7) when implemented.

## Key Management

- The shared key comes from the process boundary: `EnvironmentSentinelKeyProvider` reads the configured environment variable (default `SENTINEL_KEY`, `SentinelSecurityOptions.KeyVariableName`) **on every request**, so rotated values take effect without a restart.
- The key is never cached in static state and must never be logged.
- Stamping without a key throws `InvalidOperationException`; validation without a key returns `KeyUnavailable` and the message is quarantined. Both directions fail closed.
- Planned: a secret-store/DPAPI-backed provider and per-identity keys.

## Identity Registry (Stub)

- `ISentinelIdentityRegistry` approves who may stamp (`Register`/`Revoke`/`IsRegistered`/`TryGet`) and who a stamp claims (`RegisteredIdentity`).
- `RegisteredIdentity(Identifier, DisplayName, IdentityKind)` with `IdentityKind` `LocalOperator`, `DeployedService`, or `ExternalSource`.
- The shipped `InMemorySentinelIdentityRegistry` seeds the first-party identities at startup (per-process only):
  - `SentinelIdentities.CoreUi` = `"SentinelCore.UI"` — the WPF operator host
  - `SentinelIdentities.CoreService` = `"SentinelCoreService"` — the deployed Windows service (pathway pending)
- Stamping an unregistered identifier throws; validating one fails with `UnknownIdentity` and quarantines the message.
- Planned: a durable registry plus a registry management page in the admin UI (list, add, revoke) and an onboarding flow that issues identifiers to approved external sources.

## DI Registration

`AddSentinelSecurity(this IServiceCollection, Action<SentinelSecurityOptions>? configure = null)` registers all services as **singletons** so the replay cache and quarantine store are shared process-wide:

| Service | Implementation |
| --- | --- |
| `SentinelSecurityOptions` | Configured instance |
| `ISentinelKeyProvider` | `EnvironmentSentinelKeyProvider(options.KeyVariableName)` |
| `ISentinelIdentityRegistry` | `InMemorySentinelIdentityRegistry(options.SeedDefaultIdentities)` |
| `ISentinelProvenanceService` | `SentinelProvenanceService` |
| `IQuarantineStore` | `InMemoryQuarantineStore` (resolves `ISystemReporter` from the host) |

`AddSentinelCore` calls `AddSentinelSecurity` after executor registration. The registration never builds a service provider and never registers logging (PL-4).

## SentinelCoreService Input Pathway (Planned)

The `SentinelCoreService` Windows service is deployed with the app and is intended to send signed investigation requests to The Core. Current state:

- It already has a registered identity: `SentinelIdentities.CoreService` (`IdentityKind.DeployedService`).
- The transport pathway is **not implemented**.
- The service targets .NET Framework 4.8 and therefore cannot reference the `net10.0` `SentinelCoreSecurity` assembly. When the pathway is built, either the canonical-payload + HMAC signing is implemented inside the service against the same payload contract, or the signing core is moved to a compatible shared target. That decision belongs to the pathway work and must be recorded before implementation.
- The receiving entry point will run the consuming `Validate` at ingestion, so service messages get replay detection and full validation at the boundary.

## Limitations and Future Work

- The quarantine store and replay cache are per-process; cross-process replay detection and quarantine durability require a shared durable store.
- The identity registry is per-process; a durable registry and management UI are planned.
- One shared symmetric key covers all senders; per-identity asymmetric signing is a possible later evolution of the same payload contract.
- `AdditionalProperties` stamps are process-local metadata and are not serialized to AI providers.
- `openssl.exe` in the module folder is an unused proof-of-concept leftover, not referenced by any code; it is a candidate for deletion pending approval.

## Tests

| Test class | Coverage |
| --- | --- |
| `SentinelCore.Tests/Security/ProvenanceServiceTests.cs` | Stamp/validate round trip, tampered text, unsigned, forged identity, replay, repeatable integrity re-check, backdated stamp, key unavailable, stamping failure contracts, environment key provider read/fail-closed |
| `SentinelCore.Tests/Security/QuarantineStoreTests.cs` | As-is storage, warning without content echo, capacity eviction, `RecordQuarantined` event |
| `SentinelCore.Tests/Security/ProvenanceGateExecutorTests.cs` | Valid message passes through unchanged, unsigned message quarantined and returns null, null input reports error |
| `SentinelCore.Tests/TestInfrastructure/ReportingCapture.cs` | `ISystemReporter` fake capturing message lists per severity |

## Related Documents

- [ADR-008 — Message Provenance Security Module](./ADR/008-Message-Provenance-Security-Module.md)
- [SolutionArchitecture.md](./SolutionArchitecture.md) — project membership and dependency rules
- [../architecture/pattern-lock.md](../architecture/pattern-lock.md) — PL-10 locked rules
- [PersistenceComponent.md](./PersistenceComponent.md) — factory rules the future durable quarantine must follow

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 1.0 | 2026-11-20 | Copilot | Initial reference for the formalized provenance security module. |
