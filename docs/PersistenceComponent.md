---
title: "Persistence Component"
status: Active
component: Persistence
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Persistence Component

**Project:** `SentinelCore.CaseFlowEngine`
**Namespaces:** `SentinelCore.CaseFlowEngine.Persistence`
**Dependencies:** `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.VectorData`
**Consumers:** `SentinelCore.CaseFlowEngine` (CaseFlowEngine), `SentinelCoreHost` (via DI)

---

## Purpose

The Persistence component provides **SQL Server-backed data storage** for all case investigation artifacts. It uses Entity Framework Core with SQL Server's native `vector` type for embedding storage and similarity search.

**Entities Persisted:**
- Cases (aggregate root)
- Signals (triggering events)
- Evidence (collected artifacts)
- Investigation Plans & Steps
- Pattern Memories (vector embeddings)
- Resolutions (case outcomes)

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.CaseFlowEngine                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  Persistence Component                     │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │           SentinelCoreDbContext                      │  │  │
│  │  │  DbSet<CaseEntity> Cases                            │  │  │
│  │  │  DbSet<SignalEntity> Signals                        │  │  │
│  │  │  DbSet<EvidenceEntity> Evidence                     │  │  │
│  │  │  DbSet<InvestigationPlanEntity> Plans               │  │  │
│  │  │  DbSet<InvestigationPlanStepsEntity> PlanSteps      │  │  │
│  │  │  DbSet<PatternMemoryEntity> PatternMemories         │  │  │
│  │  │  DbSet<ResolutionEntity> Resolutions                │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  │                           │                                │  │
│  │                           ▼                                │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │           SQL Server Database                        │  │  │
│  │  │  - Tables with FK relationships                      │  │  │
│  │  │  - vector(1536) columns for embeddings               │  │  │
│  │  │  - Vector similarity search via VECTOR_DISTANCE      │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Repository Abstractions                                   │  │
│  │  - ICaseRepository                                         │  │
│  │  - ISignalRepository                                       │  │
│  │  - IEvidenceStore                                          │  │
│  │  - IPatternMemoryStore                                     │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. DbContext Configuration

**File:** `SentinelCore.CaseFlowEngine/Persistence/SentinelCoreDbContext.cs`

```csharp
public class SentinelCoreDbContext : DbContext
{
    public DbSet<CaseEntity> Cases { get; set; } = null!;
    public DbSet<SignalEntity> Signals { get; set; } = null!;
    public DbSet<EvidenceEntity> Evidence { get; set; } = null!;
    public DbSet<InvestigationPlanEntity> InvestigationPlans { get; set; } = null!;
    public DbSet<InvestigationPlanStepsEntity> InvestigationPlanSteps { get; set; } = null!;
    public DbSet<PatternMemoryEntity> PatternMemories { get; set; } = null!;
    public DbSet<ResolutionEntity> Resolutions { get; set; } = null!;

    public SentinelCoreDbContext(DbContextOptions<SentinelCoreDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Connection string from SentinelCoreSettings.SqlConnectionString
            // Configured via DI in SentinelCoreServiceExtensions
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // CaseEntity
        modelBuilder.Entity<CaseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CaseRecordId).IsUnique();
            entity.HasIndex(e => e.CaseId).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasMany(e => e.EvidenceItems)
                .WithOne(e => e.Case)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Signals)
                .WithOne(e => e.Case)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.InvestigationPlans)
                .WithOne(e => e.Case)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.PatternMemories)
                .WithOne(e => e.Case)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Resolution)
                .WithOne(e => e.Case)
                .HasForeignKey<ResolutionEntity>(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SignalEntity
        modelBuilder.Entity<SignalEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SignalId).IsUnique();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // EvidenceEntity
        modelBuilder.Entity<EvidenceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EvidenceId).IsUnique();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.ContentJson).HasColumnType("nvarchar(max)");
        });

        // InvestigationPlanEntity
        modelBuilder.Entity<InvestigationPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlanId).IsUnique();
            entity.HasMany(e => e.Steps)
                .WithOne(e => e.Plan)
                .HasForeignKey(e => e.PlanEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InvestigationPlanStepsEntity
        modelBuilder.Entity<InvestigationPlanStepsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StepId).IsUnique();
            entity.Property(e => e.Surface).HasConversion<string>();
            entity.Property(e => e.Instruction).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Result).HasColumnType("nvarchar(max)");
        });

        // PatternMemoryEntity - VECTOR COLUMNS
        modelBuilder.Entity<PatternMemoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatternId).IsUnique();
            entity.HasIndex(e => e.CaseId);

            // Vector columns for SQL Server vector search
            entity.Property(e => e.SignalEmbedding)
                .HasColumnType("vector(1536)");
            entity.Property(e => e.SummaryEmbedding)
                .HasColumnType("vector(1536)");

            entity.HasOne(e => e.Case)
                .WithMany(c => c.PatternMemories)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ResolutionEntity
        modelBuilder.Entity<ResolutionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CaseRecordId).IsUnique();
            entity.Property(e => e.RawJsonContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
        });
    }
}
```

**Key Configuration Points:**
- **Vector columns**: `vector(1536)` for SQL Server native vector similarity search
- **Cascade deletes**: Case deletion removes all related entities
- **Unique indexes**: On business keys (CaseId, SignalId, EvidenceId, PlanId, StepId, PatternId)
- **Status as string**: `CaseStatus` enum stored as string for readability

---

## 2. Entity Definitions

### CaseEntity (Aggregate Root)

**File:** `SentinelCore.CaseFlowEngine/Persistence/CaseEntity.cs`

```csharp
public class CaseEntity
{
    public int Id { get; set; }

    /// <summary>Internal record ID (Guid as string).</summary>
    public string CaseRecordId { get; set; } = string.Empty;

    /// <summary>Business case ID (human-readable).</summary>
    public string CaseId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public string? PlanId { get; set; }
    public CaseStatus Status { get; set; }
    public string? InitiatingSignal { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<EvidenceEntity> EvidenceItems { get; set; } = new List<EvidenceEntity>();
    public ICollection<SignalEntity> Signals { get; set; } = new List<SignalEntity>();
    public ICollection<InvestigationPlanEntity> InvestigationPlans { get; set; } = new List<InvestigationPlanEntity>();
    public ICollection<PatternMemoryEntity> PatternMemories { get; set; } = new List<PatternMemoryEntity>();
    public ResolutionEntity? Resolution { get; set; }
}
```

### SignalEntity

**File:** `SentinelCore.CaseFlowEngine/Persistence/SignalEntity.cs`

```csharp
public class SignalEntity
{
    public int Id { get; set; }
    public string SignalId { get; set; } = string.Empty;
    public int CaseEntityId { get; set; }
    public string SignalText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    public CaseEntity Case { get; set; } = null!;
}
```

### EvidenceEntity

**File:** `SentinelCore.CaseFlowEngine/Persistence/EvidenceEntity.cs`

```csharp
public class EvidenceEntity
{
    public int Id { get; set; }
    public int CaseEntityId { get; set; }
    public string CaseRecordId { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string Provenance { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;

    public CaseEntity Case { get; set; } = null!;
}
```

### InvestigationPlanEntity

**File:** `SentinelCore.CaseFlowEngine/Persistence/InvestigationPlanEntity.cs`

```csharp
public class InvestigationPlanEntity
{
    public int Id { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public int CaseEntityId { get; set; }

    public CaseEntity Case { get; set; } = null!;
    public ICollection<InvestigationPlanStepsEntity> Steps { get; set; } = new List<InvestigationPlanStepsEntity>();
}
```

### InvestigationPlanStepsEntity

**File:** `SentinelCore.CaseFlowEngine/Persistence/InvestigationPlanStepsEntity.cs`

```csharp
public class InvestigationPlanStepsEntity
{
    public int Id { get; set; }
    public string StepId { get; set; } = string.Empty;
    public int PlanEntityId { get; set; }
    public string Surface { get; set; } = string.Empty; // InvestigationSurface enum as string
    public string Instruction { get; set; } = string.Empty;
    public string? Result { get; set; }
    public bool CompletedSuccessfully { get; set; }
    public bool TaskBlocked { get; set; }
    public bool IsTargetPropertyMissing { get; set; }
    public string? OperationId { get; set; }

    public InvestigationPlanEntity Plan { get; set; } = null!;
}
```

### PatternMemoryEntity (Vector Storage)

**File:** `SentinelCore.CaseFlowEngine/Persistence/PatternMemoryEntity.cs`

```csharp
public class PatternMemoryEntity
{
    public int Id { get; set; }
    public string PatternId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Vector embedding of the initiating signal (1536 dimensions).</summary>
    public SqlVector<float> SignalEmbedding { get; set; } = new SqlVector<float>(1536);

    /// <summary>Vector embedding of the investigation summary (1536 dimensions).</summary>
    public SqlVector<float> SummaryEmbedding { get; set; } = new SqlVector<float>(1536);

    public int CaseEntityId { get; set; }
    public CaseEntity Case { get; set; } = null!;
}
```

### ResolutionEntity

**File:** `SentinelCore.CaseFlowEngine/Persistence/ResolutionEntity.cs`

```csharp
public class ResolutionEntity
{
    public int Id { get; set; }
    public string CaseRecordId { get; set; } = string.Empty;
    public int CaseEntityId { get; set; }
    public string RawJsonContent { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool Verified { get; set; }

    public CaseEntity Case { get; set; } = null!;
}
```

---

## 3. Repository Implementations

### CaseRepository

**File:** `SentinelCore.CaseFlowEngine/Persistence/CaseRepository.cs`
**Implements:** `SentinelCore.Contracts.Abstractions.ICaseRepository`

```csharp
public class CaseRepository : ICaseRepository
{
    private readonly SentinelCoreDbContext _context;

    public CaseRepository(SentinelCoreDbContext context)
    {
        _context = context;
    }

    public async Task<Case> CreateAsync(Case caseRecord, CancellationToken ct = default)
    {
        var entity = CaseEntity.FromDomain(caseRecord);
        _context.Cases.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<Case> CreateCaseWithSignalAsync(Case caseRecord, Signal signal, CancellationToken ct = default)
    {
        var caseEntity = CaseEntity.FromDomain(caseRecord);
        var signalEntity = SignalEntity.FromDomain(signal);

        caseEntity.Signals.Add(signalEntity);
        signalEntity.Case = caseEntity;

        _context.Cases.Add(caseEntity);
        await _context.SaveChangesAsync(ct);
        return caseEntity.ToDomain();
    }

    public async Task<Case?> GetByIdAsync(string caseId, CancellationToken ct = default)
    {
        var entity = await _context.Cases
            .Include(c => c.EvidenceItems)
            .Include(c => c.Signals)
            .Include(c => c.InvestigationPlans)
                .ThenInclude(p => p.Steps)
            .Include(c => c.PatternMemories)
            .Include(c => c.Resolution)
            .FirstOrDefaultAsync(c => c.CaseId == caseId, ct);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Case>> ListAsync(CaseStatus? status = null, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var query = _context.Cases.AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<Case> UpdateAsync(Case caseRecord, CancellationToken ct = default)
    {
        var entity = await _context.Cases
            .Include(c => c.EvidenceItems)
            .Include(c => c.Signals)
            .Include(c => c.InvestigationPlans)
                .ThenInclude(p => p.Steps)
            .Include(c => c.PatternMemories)
            .Include(c => c.Resolution)
            .FirstOrDefaultAsync(c => c.CaseId == caseRecord.CaseId, ct);

        if (entity == null)
            throw new InvalidOperationException($"Case {caseRecord.CaseId} not found");

        entity.UpdateFromDomain(caseRecord);
        await _context.SaveChangesAsync(ct);
        return entity.ToDomain();
    }
}
```

### SignalRepository

**File:** `SentinelCore.CaseFlowEngine/Persistence/SignalRepository.cs`
**Implements:** `SentinelCore.Contracts.Abstractions.ISignalRepository`

```csharp
public class SignalRepository : ISignalRepository
{
    private readonly SentinelCoreDbContext _context;

    public async Task<Signal> AddAsync(Signal signal, CancellationToken ct = default)
    {
        var entity = SignalEntity.FromDomain(signal);
        _context.Signals.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<Signal> AssignToCaseAsync(string signalId, string caseId, CancellationToken ct = default)
    {
        var entity = await _context.Signals.FirstOrDefaultAsync(s => s.SignalId == signalId, ct);
        if (entity == null)
            throw new InvalidOperationException($"Signal {signalId} not found");

        var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId, ct);
        if (caseEntity == null)
            throw new InvalidOperationException($"Case {caseId} not found");

        entity.CaseEntityId = caseEntity.Id;
        entity.Case = caseEntity;
        await _context.SaveChangesAsync(ct);
        return entity.ToDomain();
    }
}
```

### EvidenceStore

**File:** `SentinelCore.CaseFlowEngine/Persistence/EvidenceStore.cs`
**Implements:** `SentinelCore.Contracts.Abstractions.IEvidenceStore`

```csharp
public class EvidenceStore : IEvidenceStore
{
    private readonly SentinelCoreDbContext _context;

    public async Task<Evidence> AddAsync(Evidence evidence, CancellationToken ct = default)
    {
        var entity = EvidenceEntity.FromDomain(evidence);
        _context.Evidence.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(string caseId, CancellationToken ct = default)
    {
        var entities = await _context.Evidence
            .Where(e => e.CaseRecordId == caseId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }
}
```

### PatternMemoryStore

**File:** `SentinelCore.CaseFlowEngine/Persistence/PatternMemoryStore.cs`
**Implements:** `SentinelCore.Contracts.Abstractions.IPatternMemoryStore`

```csharp
public class PatternMemoryStore : IPatternMemoryStore
{
    private readonly SentinelCoreDbContext _context;

    public async Task<IReadOnlyList<PatternMemory>> SearchAsync(
        ReadOnlyMemory<float> embedding,
        int topK = 5,
        float threshold = 0.7f,
        CancellationToken ct = default)
    {
        // Convert ReadOnlyMemory<float> to SqlVector<float>
        var queryVector = new SqlVector<float>(embedding.ToArray());

        // Use SQL Server vector distance function
        var results = await _context.PatternMemories
            .FromSqlRaw(
                @"SELECT TOP (@topK) *
                  FROM PatternMemories
                  WHERE VECTOR_DISTANCE('cosine', SignalEmbedding, @queryVector) < @threshold
                  ORDER BY VECTOR_DISTANCE('cosine', SignalEmbedding, @queryVector)",
                new SqlParameter("@topK", topK),
                new SqlParameter("@queryVector", queryVector),
                new SqlParameter("@threshold", threshold))
            .Include(p => p.Case)
            .ToListAsync(ct);

        return results.Select(e => e.ToDomain()).ToList();
    }

    public async Task StoreAsync(PatternMemory memory, CancellationToken ct = default)
    {
        var entity = PatternMemoryEntity.FromDomain(memory);
        _context.PatternMemories.Add(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PatternMemory?> GetByCaseIdAsync(string caseId, CancellationToken ct = default)
    {
        var entity = await _context.PatternMemories
            .Include(p => p.Case)
            .FirstOrDefaultAsync(p => p.CaseId == caseId, ct);

        return entity?.ToDomain();
    }
}
```

---

## 4. Domain ↔ Entity Mapping

Each entity has `FromDomain` and `ToDomain` methods for conversion.

### CaseEntity Mapping

```csharp
public static CaseEntity FromDomain(Case domain)
{
    return new CaseEntity
    {
        CaseRecordId = domain.Id,
        CaseId = domain.CaseId,
        CreatedAt = domain.CreatedAt,
        PlanId = domain.PlanId,
        Status = domain.Status,
        InitiatingSignal = domain.InitiatingSignal?.SignalText,
        UpdatedAt = domain.UpdatedAt,
        EvidenceItems = domain.EvidenceItems.Select(EvidenceEntity.FromDomain).ToList(),
        Signals = domain.Signals.Select(SignalEntity.FromDomain).ToList(),
        InvestigationPlans = domain.Plans?.Select(InvestigationPlanEntity.FromDomain).ToList() ?? [],
        PatternMemories = domain.PatternMemory != null
            ? new List<PatternMemoryEntity> { PatternMemoryEntity.FromDomain(domain.PatternMemory) }
            : [],
        Resolution = domain.Resolution != null ? ResolutionEntity.FromDomain(domain.Resolution) : null
    };
}

public Case ToDomain()
{
    return new Case
    {
        Id = CaseRecordId,
        CaseId = CaseId,
        CreatedAt = CreatedAt,
        PlanId = PlanId,
        Status = Status,
        InitiatingSignal = Signals.FirstOrDefault()?.ToDomain(),
        UpdatedAt = UpdatedAt,
        EvidenceItems = EvidenceItems.Select(e => e.ToDomain()).ToList(),
        Signals = Signals.Select(s => s.ToDomain()).ToList(),
        Plans = InvestigationPlans.Select(p => p.ToDomain()).ToList(),
        PatternMemory = PatternMemories.FirstOrDefault()?.ToDomain(),
        Resolution = Resolution?.ToDomain()
    };
}
```

---

## 5. Vector Search Implementation

### SQL Server Vector Functions

```sql
-- Create vector index (run once via migration or manual SQL)
CREATE VECTOR INDEX IX_PatternMemories_SignalEmbedding
ON PatternMemories (SignalEmbedding);

CREATE VECTOR INDEX IX_PatternMemories_SummaryEmbedding
ON PatternMemories (SummaryEmbedding);

-- Similarity search query
DECLARE @queryVector vector(1536) = CAST(@embedding AS vector(1536));

SELECT TOP (@topK)
    PatternId, CaseId, Timestamp,
    VECTOR_DISTANCE('cosine', SignalEmbedding, @queryVector) AS Distance
FROM PatternMemories
WHERE VECTOR_DISTANCE('cosine', SignalEmbedding, @queryVector) < @threshold
ORDER BY Distance;
```

### EF Core Raw SQL Approach

The `PatternMemoryStore.SearchAsync` uses `FromSqlRaw` because EF Core's LINQ provider doesn't yet fully support `VECTOR_DISTANCE` translation.

---

## 6. DI Registration

**File:** `SentinelCore.CaseFlowEngine/CaseFlowEngineServiceExtensions.cs`

```csharp
public static IServiceCollection AddCaseFlowEngine(this IServiceCollection services, IConfiguration configuration)
{
    var settings = configuration.GetSection("SentinelCore").Get<SentinelCoreSettings>()
        ?? throw new InvalidOperationException("SentinelCore settings not configured");

    services.AddDbContext<SentinelCoreDbContext>(options =>
    {
        options.UseSqlServer(settings.SqlConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.CommandTimeout(30);
        });
    });

    // Repositories
    services.AddScoped<ICaseRepository, CaseRepository>();
    services.AddScoped<ISignalRepository, SignalRepository>();
    services.AddScoped<IEvidenceStore, EvidenceStore>();
    services.AddScoped<IPatternMemoryStore, PatternMemoryStore>();

    // CaseFlowEngine
    services.AddScoped<ICaseFlowEngine, CaseFlowEngine>();

    return services;
}
```

---

## 7. Database Schema Summary

| Table | Primary Key | Key Indexes | Vector Columns |
|-------|-------------|-------------|----------------|
| Cases | Id (int) | CaseRecordId (unique), CaseId (unique), Status | - |
| Signals | Id (int) | SignalId (unique), CaseEntityId (FK) | - |
| Evidence | Id (int) | EvidenceId (unique), CaseEntityId (FK) | - |
| InvestigationPlans | Id (int) | PlanId (unique), CaseEntityId (FK) | - |
| InvestigationPlanSteps | Id (int) | StepId (unique), PlanEntityId (FK) | - |
| PatternMemories | Id (int) | PatternId (unique), CaseId, CaseEntityId (FK) | SignalEmbedding (vector(1536)), SummaryEmbedding (vector(1536)) |
| Resolutions | Id (int) | CaseRecordId (unique), CaseEntityId (FK) | - |

---

## 8. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Abstractions in Contracts | ✅ | `ICaseRepository`, `ISignalRepository`, `IEvidenceStore`, `IPatternMemoryStore` |
| EF Core in CaseFlowEngine | ✅ | Implementation isolated to persistence project |
| SQL Server vector type | ✅ | `SqlVector<float>(1536)` with `vector(1536)` column |
| Cascade deletes from Case | ✅ | All child entities cascade on Case delete |
| Unique business keys | ✅ | CaseId, SignalId, EvidenceId, PlanId, StepId, PatternId |
| Status as string | ✅ | `CaseStatus` enum stored as string |

---

## 9. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| Vector index creation | Migration/SQL | ⚠️ Manual SQL needed |
| Embedding dimension constant | Shared constant | ❌ Hardcoded 1536 in multiple places |
| Connection resilience | DbContext config | ⚠️ Basic retry only |
| Migration strategy | Project | ❌ No migrations project |
| Read replicas | DI config | ❌ Not configured |
| Soft delete pattern | Entities | ❌ Hard deletes only |
| Audit logging | Entities | ❌ Not implemented |
| Partitioning for large tables | Schema | ❌ Not configured |

---

## 10. Related Documentation

| Document | Description |
|----------|-------------|
| `CaseFlowEngineComponent.md` | Case lifecycle, repositories usage |
| `MemoryLayerComponent.md` | PatternMemoryStore, vector search |
| `ContractsComponent.md` | Repository abstractions, DTOs |
| `PatternLock.md` | Architectural constraints |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
