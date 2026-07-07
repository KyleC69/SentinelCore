// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PatternMemoryStore.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;

using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.Infrastructure.Persistence.Entities;




namespace SentinelCoreLib.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IPatternMemoryStore" />.
/// </summary>
public sealed class PatternMemoryStore : IPatternMemoryStore
{
    private readonly SentinelCoreDbContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryStore" /> class.
    /// </summary>
    public PatternMemoryStore(SentinelCoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<PatternMemoryMatch>> SearchAsync(ReadOnlyMemory<float> embedding, int topK, CancellationToken cancellationToken = default)
    {
        float[] queryVector = embedding.ToArray();
        List<PatternMemoryEntity> entries = await _context.PatternMemory.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        return entries.Select(e => new PatternMemoryMatch(MapToEntry(e), CosineSimilarity(queryVector, e.Embedding))).Where(m => m.Similarity > 0).OrderByDescending(m => m.Similarity).Take(topK).ToList();
    }








    /// <inheritdoc />
    public async Task StoreAsync(PatternMemoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        PatternMemoryEntity entity = new()
        {
                EntryId = entry.EntryId,
                CaseId = entry.CaseId,
                Category = entry.Category,
                Description = entry.Description,
                Embedding = entry.Embedding.ToArray(),
                MetadataJson = entry.MetadataJson,
                Timestamp = entry.Timestamp
        };

        _context.PatternMemory.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        double denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : (float)(dot / denominator);
    }








    private static PatternMemoryEntry MapToEntry(PatternMemoryEntity entity) => new(entity.EntryId, entity.CaseId, entity.Category, entity.Description, entity.Embedding, entity.MetadataJson, entity.Timestamp);
}