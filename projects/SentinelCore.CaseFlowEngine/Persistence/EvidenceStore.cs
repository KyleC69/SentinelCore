// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.EntityFrameworkCore;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IEvidenceStore" />.
/// </summary>
public sealed class EvidenceStore : IEvidenceStore
{
    private readonly IDbContextFactory<SentinelCoreDBContext> _dbContextFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="EvidenceStore" /> class.
    /// </summary>
    /// <param name="dbContextFactory">
    ///     Factory that creates a short-lived <see cref="SentinelCoreDBContext" /> per operation.
    /// </param>
    public EvidenceStore(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }








    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    public async Task AddAsync(int caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        CaseEntity? caseRecord = await db.CaseEntities!.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (caseRecord is null)
        {
            throw new InvalidOperationException($"Case '{caseId}' not found.");
        }

        var entity = new EvidenceEntity
        {
            CaseId = caseId,
            CollectorAgent = item.CollectorAgent ?? string.Empty,
            SourcePath = item.SourcePath ?? string.Empty,
            SourceType = item.SourceType ?? string.Empty,
            Observation = item.Observation ?? string.Empty,
            ObservedAt = item.ObservedAt,
            CreatedAt = item.CreatedAt,
            Confidence = item.Confidence
        };

        db.EvidenceEntities!.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Gets all evidence items for the specified case.
    /// </summary>
    public async Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(int caseId, CancellationToken cancellationToken = default)
    {
        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var items = await db.EvidenceEntities!
            .Where(e => e.CaseId == caseId)
            .Select(e => new Evidence
            {
                Id = e.Id,
                CaseId = e.CaseId,
                CollectorAgent = e.CollectorAgent ?? string.Empty,
                SourcePath = e.SourcePath ?? string.Empty,
                SourceType = e.SourceType ?? string.Empty,
                Observation = e.Observation ?? string.Empty,
                ObservedAt = e.ObservedAt,
                CreatedAt = e.CreatedAt,
                Confidence = e.Confidence,
                // DTO-only fields — provide defaults to avoid nulls
                ContentJson = string.Empty,
                Timestamp = e.ObservedAt,
                Type = string.Empty,
                CaseTranscript = string.Empty
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }








    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    public Task AddEvidenceAsync(int caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return AddAsync(caseId, item, cancellationToken);
    }
}