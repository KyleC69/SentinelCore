// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  081602



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








    public Task AddAsync(string caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }








    Task<IReadOnlyList<Evidence>> IEvidenceStore.GetByCaseIdAsync(string caseId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }








    public async Task AddEvidenceAsync(Guid caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        CaseEntity? caseRecord = await _context.CaseEntities.AsNoTracking().FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken).ConfigureAwait(false);

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
}