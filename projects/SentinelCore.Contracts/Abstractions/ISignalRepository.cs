// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ISignalRepository.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.CaseFlow;




namespace SentinelCore.Abstractions;





/// <summary>
///     Stores and retrieves signal records.
/// </summary>
public interface ISignalRepository
{
    /// <summary>
    ///     Persists a signal and returns its auto-generated database Id.
    /// </summary>
    /// <param name="signal">The signal to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The auto-generated database Id of the persisted signal.</returns>
    Task<int> AddAsync(Signal signal, CancellationToken cancellationToken = default);
}