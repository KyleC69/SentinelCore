// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         SignalMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using SentinelCore.CaseFlowEngine.DBEntities;
using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Signal" /> contract
///     objects and <see cref="SignalEntity" /> persistence objects.
/// </summary>
public static class SignalMappingExtensions
{



    /// <summary>
    ///     Maps a <see cref="SignalEntity" /> persistence object back to its <see cref="Signal" />
    ///     contract representation. Persistence types never leave this project.
    /// </summary>
    /// <param name="entity">The persistence entity to map from.</param>
    /// <returns>A new <see cref="Signal" /> populated from <paramref name="entity" />.</returns>
    public static Signal ToContract(this SignalEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new()
        {
                Id = entity.Id,
                SignalId = entity.SignalId,
                SignalText = entity.SignalText ?? string.Empty,
                Source = entity.Source ?? string.Empty,
                Notes = entity.Notes ?? string.Empty,
                Timestamp = entity.FirstSeen
        };
    }








    /// <summary>
    ///     Maps a <see cref="Signal" /> DTO to a new <see cref="SignalEntity" />.
    /// </summary>
    /// <param name="signal">The source signal DTO to map from.</param>
    /// <returns>A new <see cref="SignalEntity" /> populated with values from <paramref name="signal" />.</returns>
    public static SignalEntity ToEntity(this Signal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        return new()
        {
                Id = signal.Id,
                CaseId = 0, // or map if the contract exposes CaseId
                SignalText = signal.SignalText,
                Source = signal.Source,
                FirstSeen = signal.Timestamp,
                Notes = signal.Notes
        };
    }
}