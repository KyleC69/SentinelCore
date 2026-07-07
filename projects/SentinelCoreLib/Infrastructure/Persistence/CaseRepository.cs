// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CaseRepository.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;

using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.Infrastructure.Persistence.Entities;




namespace SentinelCoreLib.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="ICaseRepository" />.
/// </summary>
public sealed class CaseRepository : ICaseRepository
{
    private readonly SentinelCoreDbContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="CaseRepository" /> class.
    /// </summary>
    public CaseRepository(SentinelCoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    /// <inheritdoc />
    public async Task CreateAsync(CaseRecord caseRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseRecord);
        CaseEntity entity = MapToEntity(caseRecord);
        _context.Cases.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    /// <inheritdoc />
    public async Task<CaseRecord?> GetByIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        CaseEntity? entity = await _context.Cases.AsNoTracking().FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken).ConfigureAwait(false);

        return entity is null ? null : MapToModel(entity);
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<CaseRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        List<CaseEntity> entities = await _context.Cases.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

        return entities.Select(MapToModel).ToList();
    }








    /// <inheritdoc />
    public async Task UpdateAsync(CaseRecord caseRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseRecord);
        CaseEntity? existing = await _context.Cases.FirstOrDefaultAsync(c => c.CaseId == caseRecord.CaseId, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            throw new InvalidOperationException($"Case '{caseRecord.CaseId}' not found.");
        }

        existing.Title = caseRecord.Title;
        existing.Status = caseRecord.Status.ToString();
        existing.StateJson = caseRecord.StateJson;
        existing.UpdatedAt = caseRecord.UpdatedAt;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    private static CaseEntity MapToEntity(CaseRecord record) =>
            new()
            {
                CaseId = record.CaseId,
                Title = record.Title,
                Status = record.Status.ToString(),
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                StateJson = record.StateJson
            };








    private static CaseRecord MapToModel(CaseEntity entity) => new(entity.CaseId, entity.Title, Enum.Parse<CaseStatus>(entity.Status), entity.CreatedAt, entity.UpdatedAt, entity.StateJson);
}