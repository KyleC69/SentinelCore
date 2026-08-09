// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         SignalRepository.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Abstractions;
using SentinelCore.CaseFlow;
using SentinelCore.CaseFlowEngine.Persistence;




namespace SentinelCore.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core implementation of <see cref="ISignalRepository" />.
/// </summary>
public sealed class SignalRepository : ISignalRepository
{
    private readonly SentinelCoreDBContext _context;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRepository" /> class.
    /// </summary>
    public SignalRepository(SentinelCoreDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }








    public async Task<int> AddAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        SignalEntity entity = new() { SignalId = signal.SignalId, SignalText = signal.SignalText, Source = signal.Source, Timestamp = signal.Timestamp };

        _context.SignalEntities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}