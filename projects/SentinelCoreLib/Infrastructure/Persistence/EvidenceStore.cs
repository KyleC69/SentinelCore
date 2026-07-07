// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;

using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.Infrastructure.Persistence.Entities;




namespace SentinelCoreLib.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="IEvidenceStore" />.
/// </summary>
public sealed class EvidenceStore : IEvidenceStore
{
    private readonly SentinelCoreDbContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="EvidenceStore" /> class.
    /// </summary>
    public EvidenceStore(SentinelCoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    /// <inheritdoc />
    public async Task AddAsync(string caseId, EvidenceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentNullException.ThrowIfNull(item);

        EvidenceEntity entity = new()
        {
                EvidenceId = item.EvidenceId,
                CaseId = caseId,
                Type = item.Type,
                Source = item.Source,
                ContentJson = item.ContentJson,
                Provenance = item.Provenance,
                Timestamp = item.Timestamp
        };

        _context.Evidence.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<EvidenceItem>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        List<EvidenceEntity> entities = await _context.Evidence.AsNoTracking().Where(e => e.CaseId == caseId).OrderBy(e => e.Timestamp).ToListAsync(cancellationToken).ConfigureAwait(false);

        return entities.Select(e => new EvidenceItem(e.EvidenceId, e.Type, e.Source, e.ContentJson, e.Provenance, e.Timestamp)).ToList();
    }
}