// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.EntityFrameworkCore;

using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Infrastructure.Persistence;





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
    public async Task AddAsync(string caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!Guid.TryParse(caseId, out Guid caseIdGuid) || caseIdGuid == Guid.Empty)
        {
            throw new ArgumentException("Case identifier must be a non-empty GUID string.", nameof(caseId));
        }

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        CaseEntity? caseRecord = await db.CaseEntities!.AsNoTracking().FirstOrDefaultAsync(c => c.CaseId == caseIdGuid, cancellationToken).ConfigureAwait(false);

        if (caseRecord is null)
        {
            throw new InvalidOperationException($"Case '{caseId}' not found.");
        }

        EvidenceEntity entity = new()
        {
                EvidenceId = item.EvidenceId,
                Type = item.Type,
                Source = item.Source,
                ContentJson = item.ContentJson,
                Provenance = item.Provenance,
                Timestamp = item.Timestamp
        };

        db.EvidenceEntities!.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Gets all evidence items for the specified case.
    /// </summary>
    public async Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(caseId, out Guid caseIdGuid))
        {
            return [];
        }

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<EvidenceEntity> entities = await db.EvidenceEntities!.AsNoTracking().Join(db.CaseEntities!.Where(c => c.CaseId == caseIdGuid), e => e.EvidenceId, c => c.EvidenceId, (e, c) => e).ToListAsync(cancellationToken).ConfigureAwait(false);

        return entities.Select(e => new Evidence
                {
                        Id = e.Id,
                        EvidenceId = e.EvidenceId!,
                        Type = e.Type!,
                        Source = e.Source!,
                        ContentJson = e.ContentJson!,
                        Provenance = e.Provenance!,
                        Timestamp = e.Timestamp
                })
                .ToList();
    }








    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    public Task AddEvidenceAsync(Guid caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return AddAsync(caseId.ToString(), item, cancellationToken);
    }
}