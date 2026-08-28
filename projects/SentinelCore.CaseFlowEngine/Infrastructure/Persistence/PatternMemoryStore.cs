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
    private readonly SentinelCoreDBContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryStore" /> class.
    /// </summary>
    /// <param name="context">The <see cref="SentinelCoreDBContext" /> used for persistence.</param>
    public PatternMemoryStore(SentinelCoreDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    public async Task<IReadOnlyList<PatternMemoryResult>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(caseId, out int caseRecordId))
        {
            return [];
        }

        List<PatternMemoryEntity> entities = await _context.PatternMemoryEntities
                .AsNoTracking()
                .Where(p => p.CaseId == caseRecordId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return entities.Select(ToResult).ToList();
    }








    public async Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (topK <= 0 || embedding.Length == 0)
        {
            return [];
        }

        List<PatternMemoryEntity> entities = await _context.PatternMemoryEntities
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








    public async Task StoreAsync(string caseId, string summary, float[] signalEmbedding, float[] summaryEmbedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(signalEmbedding);
        ArgumentNullException.ThrowIfNull(summaryEmbedding);

        if (!int.TryParse(caseId, out int caseRecordId))
        {
            throw new ArgumentException("Case identifier must be a numeric record identifier.", nameof(caseId));
        }

        PatternMemoryEntity entity = new()
        {
                PatternId = Guid.NewGuid().GetHashCode(),
                CaseId = caseRecordId,
                Summary = summary,
                SignalEmbedding = new SqlVector<float>(signalEmbedding),
                SummaryEmbedding = new SqlVector<float>(summaryEmbedding),
                Timestamp = DateTime.Now
        };

        _context.PatternMemoryEntities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }





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