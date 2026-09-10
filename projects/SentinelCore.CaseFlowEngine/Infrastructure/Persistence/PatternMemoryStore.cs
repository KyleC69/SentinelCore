// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         PatternMemoryStore.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

using SentinelCore.Abstractions;
using SentinelCore.Cfe.Persistence;




namespace SentinelCore.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IPatternMemoryStore" />.
/// </summary>
public sealed class PatternMemoryStore : IPatternMemoryStore
{
    private readonly IDbContextFactory<SentinelCoreDBContext> _dbContextFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryStore" /> class.
    /// </summary>
    /// <param name="dbContextFactory">
    ///     Factory that creates a short-lived <see cref="SentinelCoreDBContext" /> per operation.
    /// </param>
    public PatternMemoryStore(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }








    /// <summary>
    ///     Gets all stored pattern-memory results for the specified case record identifier.
    /// </summary>
    public async Task<IReadOnlyList<PatternMemoryResult>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(caseId, out int caseRecordId))
        {
            return [];
        }

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<PatternMemoryEntity> entities = await db.PatternMemoryEntities
                .AsNoTracking()
                .Where(p => p.CaseId == caseRecordId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return entities.Select(ToResult).ToList();
    }








    /// <summary>
    ///     Searches stored pattern memory for the entries closest to the supplied embedding.
    /// </summary>
    public async Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (topK <= 0 || embedding.Length == 0)
        {
            return [];
        }

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<PatternMemoryEntity> entities = await db.PatternMemoryEntities
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return entities
                .Select(ToResult)
                .Select(r => (Result: r, Score: CosineSimilarity(embedding, r.SignalEmbedding ?? [])))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Result)
                .ToList();
    }








    /// <summary>
    ///     Stores a pattern-memory summary together with its signal and summary embeddings.
    /// </summary>
    public async Task StoreAsync(string caseId, string summary, float[] signalEmbedding, float[] summaryEmbedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(signalEmbedding);
        ArgumentNullException.ThrowIfNull(summaryEmbedding);

        if (!int.TryParse(caseId, out int caseRecordId))
        {
            throw new ArgumentException("Case identifier must be a numeric record identifier.", nameof(caseId));
        }

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        PatternMemoryEntity entity = new()
        {
                PatternId = Guid.NewGuid().GetHashCode(),
                CaseId = caseRecordId,
                Summary = summary,
                SignalEmbedding = new SqlVector<float>(signalEmbedding),
                SummaryEmbedding = new SqlVector<float>(summaryEmbedding),
                Timestamp = DateTime.Now
        };

        db.PatternMemoryEntities.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }





    /// <summary>
    ///     Maps a pattern-memory entity to its contract result representation.
    /// </summary>
    /// <param name="e">The entity to map.</param>
    /// <returns>The mapped pattern-memory result.</returns>
    private static PatternMemoryResult ToResult(PatternMemoryEntity e)
    {
        return new PatternMemoryResult
        {
                CaseId = e.CaseId,
                PatternId = e.PatternId,
                Summary = e.Summary,
                SignalEmbedding = e.SignalEmbedding?.Memory.ToArray(),
                SummaryEmbedding = e.SummaryEmbedding?.Memory.ToArray(),
                Timestamp = e.Timestamp
        };
    }








    /// <summary>
    ///     Computes the cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="a">The query embedding vector.</param>
    /// <param name="b">The candidate embedding vector.</param>
    /// <returns>The similarity score between zero and one.</returns>
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
}