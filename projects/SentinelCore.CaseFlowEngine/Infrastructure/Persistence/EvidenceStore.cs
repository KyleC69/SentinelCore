// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.EntityFrameworkCore;

using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;

using EvidenceEntity = SentinelCore.Cfe.Persistence.EvidenceEntity;




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
    public async Task AddAsync(int caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);


        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        CaseEntity? caseRecord = await db.CaseEntities!.FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

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
    public async Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(int caseId, CancellationToken cancellationToken = default)
    {





        return default;
    }








    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    public Task AddEvidenceAsync(int caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return AddAsync(caseId, item, cancellationToken);
    }
}