## Architecture Decision Record: Package Version Pinning to the .NET 10 Wave (ADR-004)

**Status:** Accepted
**Date:** 2026-02-14
**Architects:** [Agent/Copilot]
**Impacted Components:** `Directory.Packages.props`, all projects (`SentinelCore.Contracts`, `SentinelCore.CaseFlowEngine`, `SentinelCore.Orchestrations`, `SentinelCore.UI`, `SentinelCore.Tests`)

---

### 1. Context and Problem Statement

SentinelCore targets **Windows 10 and .NET 10**. The repository instruction set states this as a
hard constraint (`Version Pinning`: *"This release is intended to be compatible with Windows 10 and
.NET 10. Do not install .NET 11 packages."*).

Before this decision, `Directory.Packages.props` contained seven package pins at **`11.0.0`**:

| Package | Was |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Abstractions` | 11.0.0 |
| `Microsoft.Extensions.Http` | 11.0.0 |
| `Microsoft.Extensions.Logging.Abstractions` | 11.0.0 |
| `Microsoft.Extensions.AI` | 11.0.0 |
| `Microsoft.Extensions.AI.Abstractions` | 11.0.0 |
| `Microsoft.Extensions.DependencyInjection` | 11.0.0 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 11.0.0 |

Those pins did not fail the build, because every `11.0.0` package also ships a `net10.0` asset. They
were nevertheless wrong for two reasons:

1. They violated the explicit .NET 10 pin.
2. They contradicted the Microsoft Agent Framework wave. `Microsoft.Agents.AI 1.28.0` (the `net10.0`
   dependency group) declares:

   ```
   Microsoft.Extensions.AI 10.11.0
   Microsoft.Extensions.AI.Abstractions 10.11.0
   Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13
   Microsoft.Extensions.Logging.Abstractions 10.0.13
   ```

   The `11.0.0` pins therefore forced MAF onto a newer `Microsoft.Extensions.AI` major than the one
   it was compiled and tested against.

A second problem was inconsistency: the remaining `Microsoft.Extensions.*` and
`Microsoft.EntityFrameworkCore.*` pins sat at `10.0.14` while their siblings were at `11.0.0`, so the
resolved graph mixed two release waves with no stated rule explaining which was correct.

---

### 2. Decision

**The .NET release wave is capped at major version 10.**

For any package whose major version tracks the .NET release wave, the pinned version must be the
latest `10.x` release. This applies to:

- `Microsoft.Extensions.*`
- `System.*`
- `Microsoft.EntityFrameworkCore.*`
- `Microsoft.Windows.Compatibility`

Every other package (MAF, ModelContextProtocol, OpenAI, OllamaSharp, MSTest, Moq, OpenTelemetry,
`Microsoft.Data.SqlClient`, UI libraries) is pinned to its **latest stable** release, because their
major versions are independent of the .NET wave.

The seven `11.0.0` pins were corrected to their latest `10.x` equivalents. No new `11.x` pin may be
added. The rule is recorded as a comment at the top of `Directory.Packages.props` so it is visible at
the exact point where a future bump would be made.

---

### 3. Consequences

#### Positive

- The dependency graph is single-wave: `Microsoft.Extensions.AI 10.11.0` matches MAF 1.28.0's declared
  minimum exactly, and `Microsoft.Extensions.*` / `System.*` / EF Core all resolve at `10.0.15`.
- The repository instruction is satisfied and self-documenting.
- Upgrades stay available: `10.0.15` is the current servicing release of the .NET 10 wave, so the
  project keeps receiving patches without crossing the major boundary.

#### Negative / accepted costs

- **`System.Diagnostics.DiagnosticSource 11.0.0` still resolves transitively.** It is a hard
  dependency of `OpenTelemetry.Api 1.21.0` (the latest OpenTelemetry release), which reaches the graph
  through `OpenTelemetry.Exporter.Console 1.21.0`. It cannot be removed without downgrading
  OpenTelemetry. It is accepted because the package ships a `net10.0` asset, so it is functionally
  .NET 10 compatible. Forcing it down to `10.x` would raise `NU1605` (dependency downgrade) and was
  rejected.
- **`Microsoft.Management.Infrastructure` is held at `3.0.0`.** Version `4.0.0` ships a bare
  `lib/Microsoft.Management.Infrastructure.dll` with no target-framework folder, which restores as a
  `.NET Framework` asset and raises `NU1701` against `net10.0-windows`. `3.0.0` supplies
  `ref/netstandard1.6` reference assemblies and is the correct choice for .NET 10.
- **`Microsoft.Agents.AI.Mcp` is held at `1.27.0`.** No stable `1.27.x`/`1.28.x` build is published on
  nuget.org; only `-alpha` builds exist.
- **`Microsoft.Windows.SDK.Contracts` / `.Ref` are held at `10.0.22621.3233`.** Later builds
  (`10.0.28000.x`) track the Windows 11 SDK, which is outside the Windows 10 target.
- **`Microsoft.EntityFrameworkCore.SqlServer.Vector` (10.0.11) and `ColorJsonProvider` (1.0.0)** are
  not published on nuget.org and cannot be resolved for upgrade from the configured feeds.
- **`Newtonsoft.Json 14.0.1-beta2` and `Azure.AI.OpenAI 2.9.0-beta.1` are already the newest available
  version.** Both are prereleases, but each is newer than that package's latest stable
  (`13.0.4` and `2.1.0` respectively), so no stable upgrade path exists.

---

### 4. Verification

- `dotnet restore SentinelCore.slnx` and the `SentinelCore.Tests` project: clean, no `NU1701`/`NU1605`.
- `dotnet build SentinelCore.slnx --no-incremental`: **0 errors, 52 warnings** (unchanged from baseline).
- `dotnet build SentinelCore.Tests.csproj --no-incremental`: **0 errors, 53 warnings**.
- `dotnet test`: **87 passed, 7 failed** — identical to the pre-upgrade baseline. The 7 failures are
  pre-existing and unrelated (5 × `ChatMessagesTests` shared-state, 1 × PL-7 EF architecture test,
  1 × missing `IEvidenceStore` DI registration).
- `AgentFactoryToolTests`: **10/10 passed**.
- At the time of this decision, `dotnet --version` resolved to `10.0.404`. The repository now contains `global.json`; use that file as the current SDK pin rather than the historical command output in this ADR.

---

### 5. Rules for Future Upgrades

1. Query the **latest `10.x`** release for any `.NET`-wave package, never the raw latest.
2. Before adopting a new MAF release, re-read its `.nuspec` `net10.0` dependency group and pin the
   `Microsoft.Extensions.AI` wave to the version MAF declares.
3. Before bumping any package whose `lib/` folder is not organised by target framework, verify it
   restores without `NU1701`.
4. A package that only publishes prereleases, or that is not present on the configured feeds, is
   documented as held rather than silently skipped.
