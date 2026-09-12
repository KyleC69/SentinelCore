// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         SignalRepository.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using Microsoft.EntityFrameworkCore;

using SentinelCore.CaseFlowEngine.Persistence;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Persistence;




namespace SentinelCore.CaseFlowEngine.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="ISignalRepository" />.
/// </summary>
public sealed class SignalRepository : ISignalRepository
{
    private readonly IDbContextFactory<SentinelCoreDBContext> _dbContextFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRepository" /> class.
    /// </summary>
    /// <param name="dbContextFactory">
    ///     Factory that creates a short-lived <see cref="SentinelCoreDBContext" /> per operation.
    /// </param>
    public SignalRepository(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }








    /// <summary>
    ///     Persists a signal and returns its generated record identifier.
    /// </summary>
    public async Task<int> AddAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        SignalEntity entity = signal.ToEntity();

        db.SignalEntities.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}