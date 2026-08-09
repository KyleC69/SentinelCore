---
title: "Memory Layer Component"
status: Active
component: MemoryLayer
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Memory Layer Component

**Project:** `SentinelCore.Contracts` (abstractions) + `SentinelCore.CaseFlowEngine` (implementation)
**Namespaces:** `SentinelCore.Contracts.Abstractions`, `SentinelCore.CaseFlowEngine.CaseFlow`, `SentinelCore.CaseFlowEngine.Persistence`
**Dependencies:** `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.VectorData`
**Consumers:** `SentinelCore.CaseFlowEngine` (CaseFlowEngine), `SentinelCore.Orchestrations` (CoreAgentFactory via PatternMemoryInjector)

---

## Purpose

The Memory Layer provides **vector-based pattern memory** for case investigations. It enables the Core agent to:
1. **Store** investigation patterns (signal + summary embeddings) after case resolution
2. **Search** for similar historical patterns when new signals arrive
3. **Inject** relevant pattern context into the Core agent's prompt via `PatternMemoryInjector`

This implements **case-based reasoning** — learning from past investigations to accelerate future ones.

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.CaseFlowEngine                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  Memory Layer Component                    │  │
│  │  ┌──────────────────┐    ┌────────────────────────────┐   │  │
│  │  │ IPatternMemoryStore│◀──│ PatternMemoryEntity        │   │  │
│  │  │ (Abstraction)     │    │ (EF Core Entity)           │   │  │
│  │  │  - SearchAsync    │    │  - SignalEmbedding:        │   │  │
│  │  │  - StoreAsync     │    │    SqlVector<float>(1536)  │   │  │
│  │  │  - GetByCaseIdAsync│   │  - SummaryEmbedding:       │   │  │
│  │  └────────┬──────────┘    │    SqlVector<float>(1536)  │   │  │
│  │           │               └──────────────┬─────────────┘   │  │
│  │           │                              │                 │  │
│  │           ▼                              ▼                 │  │
│  │  ┌────────────────────────────────────────────────────┐   │  │
│  │  │           SentinelCoreDbContext                     │   │  │
│  │  │  DbSet<PatternMemoryEntity> PatternMemories        │   │  │
│  │  │  SQL Server + SqlVector<float> for vector search   │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  IPatternMemoryStore (Abstraction)                         │  │
│  │  - SearchAsync(embedding, topK, threshold)                 │  │
│  │  - StoreAsync(PatternMemory)                               │  │
│  │  - GetByCaseIdAsync(caseId)                                │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  PatternMemory (DTO)                                       │  │
│  │  - Case, CaseId, Id, PatternId                             │  │
│  │  - SignalEmbedding: ReadOnlyMemory<float>                  │  │
│  │  - SummaryEmbedding: ReadOnlyMemory<float>                 │  │
│  │  - Timestamp                                               │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ consumed by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  PatternMemoryInjector (MessageAIContextProvider)         │  │
│  │  - Injects pattern context into Core agent messages       │  │
│  │  - Uses IPatternMemoryStore.SearchAsync                   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Abstractions (Contracts)

### IPatternMemoryStore

**File:** `SentinelCore.Contracts/Abstractions/IPatternMemoryStore.cs`

```csharp
public interface IPatternMemoryStore
{
    /// <summary>
    /// Searches for similar pattern memories using vector similarity.
    /// </summary>
    /// <param name="embedding">The query embedding vector (typically 1536 dimensions).</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (0.0-1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pattern memories ordered by similarity (highest first).</returns>
    Task<IReadOnlyList<PatternMemory>> SearchAsync(
        ReadOnlyMemory<float> embedding,
        int topK = 5,
        float threshold = 0.7f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new pattern memory with its embeddings.
    /// </summary>
    Task StoreAsync(PatternMemory memory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pattern memory by case ID.
    /// </summary>
    Task<PatternMemory?> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default);
}
```

### PatternMemory (DTO)

**File:** `SentinelCore.Contracts/CaseFlow/PatternMemory.cs`

```csharp
public sealed record PatternMemory
{
    /// <summary>The case this pattern memory belongs to.</summary>
    public Case? Case { get; init; }

    /// <summary>The case identifier.</summary>
    public string CaseId { get; init; } = string.Empty;

    /// <summary>Unique identifier for this pattern memory record.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Unique pattern identifier.</summary>
    public string PatternId { get; init; } = string.Empty;

    /// <summary>Vector embedding of the initiating signal (1536 dimensions).</summary>
    public ReadOnlyMemory<float> SignalEmbedding { get; init; } = ReadOnlyMemory<float>.Empty;

    /// <summary>Vector embedding of the investigation summary/resolution (1536 dimensions).</summary>
    public ReadOnlyMemory<float> SummaryEmbedding { get; init; } = ReadOnlyMemory<float>.Empty;

    /// <summary>When this pattern was stored.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}
```

**Key Design Decisions:**
- **Two embeddings**: Signal (what triggered the case) + Summary (what was learned)
- **1536 dimensions**: Matches common embedding models (e.g., `text-embedding-3-small`, `all-MiniLM-L6-v2`)
- **ReadOnlyMemory<float>**: Zero-copy, efficient for vector operations
- **Case reference**: Links back to full case for context retrieval

---

## 2. Persistence Implementation (CaseFlowEngine)

### PatternMemoryEntity (EF Core Entity)

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

    // Navigation
    public int CaseEntityId { get; set; }
    public CaseEntity Case { get; set; } = null!;
}
```

**Key Points:**
- `SqlVector<float>(1536)` — SQL Server vector type for native vector similarity search
- Fixed 1536 dimensions — must match embedding model output
- FK to `CaseEntity` — enables case-to-pattern navigation

### SentinelCoreDbContext Configuration

**File:** `SentinelCore.CaseFlowEngine/Persistence/SentinelCoreDbContext.cs`

```csharp
public class SentinelCoreDbContext : DbContext
{
    public DbSet<PatternMemoryEntity> PatternMemories { get; set; } = null!;
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PatternMemoryEntity configuration
        modelBuilder.Entity<PatternMemoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatternId).IsUnique();
            entity.HasIndex(e => e.CaseId);

            // Vector columns configured for SQL Server vector search
            entity.Property(e => e.SignalEmbedding)
                .HasColumnType("vector(1536)");
            entity.Property(e => e.SummaryEmbedding)
                .HasColumnType("vector(1536)");

            entity.HasOne(e => e.Case)
                .WithMany(c => c.PatternMemories)
                .HasForeignKey(e => e.CaseEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

**Vector Search Capability:**
```sql
-- SQL Server vector similarity search (cosine distance)
SELECT TOP (@topK) *
FROM PatternMemories
WHERE VectorDistance('cosine', SignalEmbedding, @queryEmbedding) < @threshold
ORDER BY VectorDistance('cosine', SignalEmbedding, @queryEmbedding);
```

---

## 3. PatternMemoryInjector (Orchestrations Integration)

**File:** `SentinelCore.Orchestrations/Agents/Middleware/PatternMemoryInjector.cs`

```csharp
public sealed class PatternMemoryInjector : MessageAIContextProvider
{
    private readonly IPatternMemoryStore _patternMemoryStore;
    private readonly ILogger<PatternMemoryInjector> _logger;

    public PatternMemoryInjector(
        IPatternMemoryStore patternMemoryStore,
        ILogger<PatternMemoryInjector> logger)
    {
        _patternMemoryStore = patternMemoryStore;
        _logger = logger;
    }

    protected override async ValueTask<IEnumerable<AIContext>> GetContextAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        // Extract the latest user message as query
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage is null)
            return [];

        // Generate embedding for the query (would use embedding service)
        // For now, placeholder - actual embedding generation happens elsewhere
        var queryEmbedding = GenerateEmbedding(lastUserMessage.Text);

        // Search for similar patterns
        var patterns = await _patternMemoryStore.SearchAsync(
            queryEmbedding,
            topK: 3,
            threshold: 0.75f,
            cancellationToken);

        if (patterns.Count == 0)
            return [];

        // Build context message with pattern summaries
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("## Relevant Historical Patterns");
        contextBuilder.AppendLine("The following patterns from past investigations may be relevant:");

        foreach (var pattern in patterns)
        {
            contextBuilder.AppendLine($"- Pattern {pattern.PatternId}: Case {pattern.CaseId}");
            if (pattern.Case?.Resolution != null)
            {
                contextBuilder.AppendLine($"  Resolution: {pattern.Case.Resolution.Notes}");
            }
        }

        return [new AIContext(contextBuilder.ToString())];
    }
}
```

**Integration Point:** Registered in `AgentBuilder` for `AgentRole.Core` only:

```csharp
// In AgentBuilder.Build()
if (spec.Role == AgentRole.Core)
{
    var patternInjector = services.GetRequiredService<PatternMemoryInjector>();
    agent = agent.WithMiddleware(patternInjector);
}
```

---

## 4. Data Flow

### Store Pattern (After Case Resolution)

```
CaseFlowEngine.AdvanceCaseAsync(caseId, CaseStatus.Resolved)
    │
    ▼
Generate embeddings for:
  - Case.InitiatingSignal.SignalText → SignalEmbedding
  - Case.Resolution.Notes → SummaryEmbedding
    │
    ▼
Create PatternMemory DTO
    │
    ▼
IPatternMemoryStore.StoreAsync(patternMemory)
    │
    ▼
EF Core: INSERT INTO PatternMemories (PatternId, CaseId, SignalEmbedding, SummaryEmbedding)
```

### Search Patterns (During Core Agent Reasoning)

```
Core Agent receives new signal
    │
    ▼
PatternMemoryInjector.GetContextAsync(messages)
    │
    ▼
Extract last user message → Generate embedding
    │
    ▼
IPatternMemoryStore.SearchAsync(embedding, topK: 3, threshold: 0.75)
    │
    ▼
SQL Server: VectorDistance('cosine', SignalEmbedding, @query) < 0.75
    │
    ▼
Return top 3 PatternMemory DTOs
    │
    ▼
Inject as AIContext into Core agent's message history
    │
    ▼
Core agent reasons with historical pattern context
```

---

## 5. Embedding Generation (External Dependency)

**Not implemented in this component** — requires an embedding service.

### Expected Interface

```csharp
public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<ReadOnlyMemory<float>[]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
```

### Integration Points Needed

| Location | Purpose |
|----------|---------|
| `CaseFlowEngine.AdvanceCaseAsync` | Generate embeddings when storing pattern |
| `PatternMemoryInjector.GetContextAsync` | Generate embedding for query signal |
| DI Registration | Register embedding service (Ollama, OpenAI, etc.) |

### Recommended Models (1536 dims)

| Model | Dimensions | Provider |
|-------|------------|----------|
| `text-embedding-3-small` | 1536 | OpenAI |
| `text-embedding-ada-002` | 1536 | OpenAI |
| `all-MiniLM-L6-v2` | 384 | SentenceTransformers (needs dimension adjustment) |
| `nomic-embed-text` | 768 | Ollama (needs dimension adjustment) |

---

## 6. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Abstraction in Contracts | ✅ | `IPatternMemoryStore`, `PatternMemory` DTO |
| Implementation in CaseFlowEngine | ✅ | EF Core entity + DbContext |
| Vector search via SQL Server | ✅ | `SqlVector<float>(1536)` |
| Core agent integration | ✅ | `PatternMemoryInjector` middleware |
| Two-embedding design (signal + summary) | ✅ | Enables both trigger and outcome matching |
| Fixed 1536 dimensions | ⚠️ | Must match embedding model |
| Embedding service abstraction | ❌ | Not yet defined in Contracts |

---

## 7. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| `IEmbeddingService` abstraction | Contracts | ❌ Not defined |
| Embedding generation in `AdvanceCaseAsync` | CaseFlowEngine | ❌ NotImplementedException |
| Embedding generation in `PatternMemoryInjector` | Orchestrations | ⚠️ Placeholder |
| Vector index creation | DbContext migration | ⚠️ Manual SQL needed |
| Similarity threshold tuning | SearchAsync call sites | ⚠️ Hardcoded 0.75 |
| Pattern memory TTL/cleanup | Store implementation | ❌ Not implemented |
| Cross-case pattern aggregation | Search logic | ❌ Not implemented |

---

## 8. Related Documentation

| Document | Description |
|----------|-------------|
| `CaseFlowEngineComponent.md` | Case lifecycle, AdvanceCaseAsync, pattern storage trigger |
| `DynamicAgentsComponent.md` | Core agent construction, PatternMemoryInjector middleware |
| `PersistenceComponent.md` | SentinelCoreDbContext, all entities, SQL Server config |
| `ContractsComponent.md` | IPatternMemoryStore, PatternMemory DTO, abstractions |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
