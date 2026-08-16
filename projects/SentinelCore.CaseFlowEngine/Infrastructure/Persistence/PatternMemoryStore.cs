// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         PatternMemoryStore.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Abstractions;
using SentinelCore.Cfe.Persistence;




namespace SentinelCore.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IPatternMemoryStore" />.
/// </summary>
public sealed class PatternMemoryStore : IPatternMemoryStore
{
    private SentinelCoreDBContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryStore" /> class.
    /// </summary>
    /// <param name="context">The <see cref="SentinelCoreDBContext" /> used for persistence.</param>
    public PatternMemoryStore(SentinelCoreDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    public Task<IReadOnlyList<PatternMemoryResult>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }








    public Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }








    public Task StoreAsync(string caseId, string summary, float[] signalEmbedding, float[] summaryEmbedding, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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