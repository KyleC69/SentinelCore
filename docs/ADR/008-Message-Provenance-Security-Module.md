## Architecture Decision Record: Message Provenance Security Module (ADR-008)

**Status:** Accepted

**Date:** 2026-11-20

**Architects:** [Agent/Copilot]

**Impacted Components:** `SentinelCoreSecurity` (new project), `SentinelCore.Orchestrations` (`TheCoreWorkflow`, `ProvenanceGateExecutor`, `TheCoreSwitchExec`, DI wiring), `SentinelCore.UI` (`CoreChatViewModel`), `SentinelCore.Contracts`, `SentinelCore.Tests`

---

### 1. Context and Problem Statement

SentinelCore is a multi-agent AI investigation platform built on MAF workflows and OllamaSharp chat clients. It is intended to accept investigation input from several external sources, and the AI agents must be protected from malicious or injected content entering the system.

A proof-of-concept security module existed before this decision. It consisted of loose files with no project wiring, no enforced validation, and a scratch `openssl.exe`. It proved the idea but was not integrated.

Before this change there was no way to prove where a message came from, no tamper detection, no replay protection, and no containment path: any message reaching the workflow entered the model pipeline regardless of origin.

The following requirements were established for the formalization:

1. Prove provenance of every message entering the system through normal pathways, and sign every such message.
2. Use the same stamping mechanism recently added for ChatMode: metadata attached to `ChatMessage` via the `Microsoft.Extensions.AI` `ChatMessage.AdditionalProperties` map (extension methods stamp in place and return the same instance).
3. Validate signatures before any model ever sees a message, at more than one point in the system (not a single choke point).
4. Any message entering through unapproved points, or lacking or invalidating a signature, is quarantined exactly as it arrived (no sanitization, no rebuild) and the event is logged with details.
5. The signing key comes from the environment variable `SENTINEL_KEY`.
6. The stamped identifier is validated against a private identity registry, which is a stub for now.
7. `SentinelCoreService` (a Windows service deployed with the app) carries its own separate identifier and sends signed messages to The Core; that transport pathway is not yet implemented.
8. Scope: formalize the security module itself plus design documentation.

### 2. Decision

#### 2.1 Module project and dependency boundary

The security module is a new solution project, `SentinelCoreSecurity`, living in the sibling solution folder `SentinelCoreSecurity/` outside the repository root and registered as a solution member of `SentinelCore.slnx`. It targets `net10.0` with root namespace `SentinelCore.Security`. Project references: `SentinelCore.Contracts` only, plus the NuGet packages `Microsoft.Extensions.AI.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions` (central package management). `InternalsVisibleTo("SentinelCore.Tests")` is granted for deterministic stamp construction in tests. This keeps the module at the contract boundary: it has no dependency on Orchestrations, UI, CaseFlowEngine, or hosts.

#### 2.2 Signature scheme and service surface

Messages are signed with HMAC-SHA256 symmetric signatures over a canonical payload, using a shared key provided at the process boundary. `SentinelProvenanceService` implements `ISentinelProvenanceService` with three operations:

- `Stamp(ChatMessage, identifier)`: signs and stamps a message.
- `Validate(ChatMessage)`: consuming validation, used at entry points.
- `VerifyIntegrity(ChatMessage)`: non-consuming verification, used for downstream re-checks.

#### 2.3 Stamp shape and storage

The stamp is `MessageProvenance`, a sealed record with `Identifier`, `TimestampUtc`, `Nonce`, and `Signature`. It is stored under the `AdditionalProperties` key `"provenance"` through the `WithProvenance`/`GetProvenance` extensions, which is the same mechanism as the ChatMode stamp (`WithChatMode`).

#### 2.4 Canonical payload and signing

The canonical payload is, in order: identifier, RFC 3339 / round-trip timestamp (`DateTimeOffset.ToString("O", InvariantCulture)`), Base64 nonce (16 random bytes from `RandomNumberGenerator`), chat role value (`ChatRole.Value`), and message text, joined with `"\n"`. The payload is signed as HMAC-SHA256 over its UTF-8 bytes with the UTF-8 key; the signature is stored Base64 and compared with `CryptographicOperations.FixedTimeEquals` (constant time).

#### 2.5 Identity registry

The identity registry is `ISentinelIdentityRegistry` plus `RegisteredIdentity(Identifier, DisplayName, IdentityKind)`, with `IdentityKind` = `LocalOperator` | `DeployedService` | `ExternalSource`. The shipped implementation is an in-memory stub (`InMemorySentinelIdentityRegistry`) seeded with the first-party identities `SentinelIdentities.CoreUi = "SentinelCore.UI"` (LocalOperator) and `SentinelIdentities.CoreService = "SentinelCoreService"` (DeployedService). Stamping an unregistered identifier throws; validating one fails validation. A durable, UI-managed registry is planned future work.

#### 2.6 Fail-closed validation order

Validation is fail-closed and runs in this exact order; each step short-circuits to a failed `ProvenanceValidationResult`:

1. Null message → `MissingProvenance`.
2. No stamp → `MissingProvenance`.
3. Empty `Identifier`/`Nonce`/`Signature` → `MalformedProvenance`.
4. Key unavailable → `KeyUnavailable`.
5. Identifier not registered → `UnknownIdentity`.
6. Timestamp outside ±`TimestampTolerance` (default 5 minutes, covering clock skew in both directions) → `ExpiredTimestamp`.
7. HMAC mismatch → `InvalidSignature`.
8. `Validate` only: nonce already consumed (`ConcurrentDictionary.TryAdd` fails) → `ReplayDetected`.

The nonce cache prunes entries older than twice the tolerance once it exceeds `MaxReplayCacheSize` (default 4096).

#### 2.7 Key management

`EnvironmentSentinelKeyProvider` reads the configured variable (default `SENTINEL_KEY`) on every request, so rotation takes effect without restart. The key is never cached in static state and must never be logged. Stamping without a key throws `InvalidOperationException` (signing fails loudly rather than producing unverifiable stamps); validation without a key returns `KeyUnavailable` and the message is quarantined.

#### 2.8 Quarantine

`IQuarantineStore.Quarantine(message, reason, detail)` returns a `QuarantineRecord(QuarantinedAtUtc, Reason, Detail, Message)` retaining the message reference exactly as it arrived. `InMemoryQuarantineStore` is bounded (default 500, oldest evicted), logs through `ISystemReporter.ReportWarning` as `[SentinelSecurity] Message quarantined. Reason: <reason>. Detail: <detail>` (deliberately excluding message content, because the content may be malicious and must not be echoed to logs), and raises a `RecordQuarantined` event for future UI consumers. The store is the single source of quarantine logging; executors must not duplicate the report.

#### 2.9 Workflow enforcement (MAF)

`ProvenanceGateExecutor` (PL-9 conformant: `[YieldsOutput(typeof(ChatMessage))]`, sealed partial, conventional constructor, `[MessageHandler] HandleChatMessageAsync` returning `ValueTask<ChatMessage?>`) is the first node of `TheCoreWorkflow`. Its outgoing conditional edge `AddEdge<ChatMessage>(gate, SafetyExecutor, ProvenanceGatePassed)` routes only non-null output: under MAF semantics the handler return value flows across conditional edges, while `YieldOutputAsync` only feeds the UI output stream. A rejected message is quarantined, yields a user-visible rejection notice, and returns null; no outgoing edge matches null, so the message dead-ends (fail-closed). Additionally, `TheCoreSwitchExec` re-checks `VerifyIntegrity` immediately before the first model contact (defense in depth; catches in-process tampering after the gate).

#### 2.10 Entry-point stamping

The operator UI (`CoreChatViewModel.SendAsync`) stamps every outgoing message with `ISentinelProvenanceService.Stamp(msg, SentinelIdentities.CoreUi)` after the ChatMode stamp.

#### 2.11 Dependency injection and options

`AddSentinelSecurity(this IServiceCollection, Action<SentinelSecurityOptions>?)` registers all five services as singletons (options, key provider, registry, provenance service, quarantine store) so the replay cache and quarantine store are process-shared; it is called by `AddSentinelCore` after executor registration. There is no `BuildServiceProvider` and no `AddLogging` (PL-4); the quarantine store resolves `ISystemReporter` supplied by the host. Options (`SentinelSecurityOptions`): `KeyVariableName` (default `SENTINEL_KEY`), `TimestampTolerance` (5 minutes), `QuarantineCapacity` (500), `MaxReplayCacheSize` (4096), `SeedDefaultIdentities` (true).

#### 2.12 SentinelCoreService pathway

`SentinelCoreService` targets .NET Framework 4.8 and cannot reference the net10.0 module assembly. It already has a registered separate identifier (`SentinelCoreService`). The transport and its signing implementation are deferred; the receiving entry point will run the consuming `Validate` at ingestion. The implementation approach (port the canonical payload + HMAC into the service, or retarget shared code) is an open decision for the pathway work.

### 3. Consequences

**Positive**

- Provenance for every approved-pathway message.
- Tamper detection over identifier + timestamp + nonce + role + text.
- Replay detection within the tolerance window.
- Unapproved, unsigned, or tampered messages are contained: quarantined as-is, user-visible rejection, and they never reach a model.
- Same stamping mechanism as ChatMode; no new message-envelope type.
- The module adds only AI/DI abstraction package dependencies.
- Compliant with PL-4 (no provider building), PL-6 (no `Console.WriteLine`), PL-9 (executor convention), and PL-1 (contracts-only dependency).

**Negative / accepted**

- Single shared symmetric key: the trust domain is process-wide, and key compromise allows forging by anyone with the key. Accepted for first-party deployment; per-identity asymmetric signing is deferred.
- The nonce cache and quarantine store are per-process, so cross-process replay and restart persistence are not covered yet.
- In-memory quarantine is lost on restart; durable persistence is planned.
- `AdditionalProperties` metadata is local to the process and is not serialized to AI providers.

**Neutral**

- The gate's nullable return plus conditional-edge filter is now a locked pattern (PL-10 in `architecture/pattern-lock.md`) for any future rejecting executor.

### 4. Alternatives Considered

| Alternative | Rejected because |
| --- | --- |
| Asymmetric per-identity signatures (RSA/Ed25519) | Stronger non-repudiation and per-sender revocation, but key distribution, rotation, and a more complex registry are not justified for a single first-party trust domain today; the payload contract does not preclude adding asymmetric verification later. |
| JSON Web Signature / detached JWS envelope | Introduces a new dependency and an envelope type foreign to `ChatMessage`; conflicts with the required `AdditionalProperties` stamping mechanism. |
| Validate at a single choke point only | Violates the multi-point validation requirement and leaves an in-process tamper window between entry and model. |
| Sanitize or rebuild rejected messages before storing | Quarantine must preserve the message as-is for evidence and forensics; sanitization would destroy provenance of the attack. |
| Throw on validation failure from the executor | Aborts the whole workflow without user feedback and is drift against PL-9's executor convention; null-return plus conditional edge keeps the graph deterministic and fail-closed. |
| Build the durable registry and durable quarantine queue now | Out of scope for formalizing the PoC; the shipped abstractions (`ISentinelIdentityRegistry`, `IQuarantineStore`) allow durable implementations without breaking changes. |

### 5. References

- `docs/SecurityComponent.md` (component reference, same date)
- `../SentinelCoreSecurity/` module folder: `SentinelProvenanceService.cs`, `Provenance.cs`, `ISentinelProvenanceService.cs`, `SentinelSecurityOptions.cs`, `Identity/`, `Cryptography/`, `Quarantine/`, `Infrastructure/DependencyInjection/SentinelSecurityServiceExtensions.cs`
- `projects/SentinelCore.Orchestrations/Workflows/Executors/ProvenanceGateExecutor.cs`
- `projects/SentinelCore.Orchestrations/Workflows/TheCoreWorkflow.cs`
- `projects/SentinelCore.Orchestrations/Workflows/Executors/TheCoreSwitchExec.cs`
- `projects/SentinelCore.UI/ViewModels/CoreChatViewModel.cs`
- `projects/SentinelCore.Orchestrations/Infrastructure/DependencyInjection/SentinelCoreServiceExtensions.cs`
- `projects/SentinelCore.Tests/Security/` (`ProvenanceServiceTests`, `QuarantineStoreTests`, `ProvenanceGateExecutorTests`)
- `architecture/pattern-lock.md` (PL-10)
- ADR-006 (`006-Tool-Group-Prefix-Scheme.md`, format reference)
