// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.EntityFrameworkCore;

using SentinelCore.Abstractions;
using SentinelCore.Cfe;
using SentinelCore.Cfe.Persistence;




namespace SentinelCore.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IEvidenceStore" />.
/// </summary>
public sealed class EvidenceStore : IEvidenceStore
{
    private readonly SentinelCoreDBContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="EvidenceStore" /> class.
    /// </summary>
    /// <param name="context">The <see cref="SentinelCoreDBContext" /> used for persistence.</param>
    public EvidenceStore(SentinelCoreDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

        CaseEntity? caseRecord = await _context.CaseEntities.AsNoTracking().FirstOrDefaultAsync(c => c.CaseId == caseIdGuid, cancellationToken).ConfigureAwait(false);

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

        _context.EvidenceEntities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        List<EvidenceEntity> entities = await _context.EvidenceEntities
                .AsNoTracking()
                .Where(e => _context.CaseEntities.Any(c => c.CaseId == caseIdGuid && c.EvidenceId == e.EvidenceId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return entities.Select(e => new Evidence
        {
                Id = e.Id,
                EvidenceId = e.EvidenceId,
                Type = e.Type,
                Source = e.Source,
                ContentJson = e.ContentJson,
                Provenance = e.Provenance,
                Timestamp = e.Timestamp
        }).ToList();
    }








    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    public Task AddEvidenceAsync(Guid caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return AddAsync(caseId.ToString(), item, cancellationToken);
    }
}