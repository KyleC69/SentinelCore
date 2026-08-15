// Solution: SentinelCore
// Project:   SentinelCore.Cfe
// File:         SignalMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.Cfe;




namespace SentinelCore.Cfe.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Signal" /> contract
///     objects and <see cref="SignalEntity" /> persistence objects.
/// </summary>
public static class SignalMappingExtensions
{
    /// <summary>
    ///     Maps a <see cref="Signal" /> DTO to a new <see cref="SignalEntity" />.
    /// </summary>
    /// <param name="signal">The source signal DTO to map from.</param>
    /// <returns>A new <see cref="SignalEntity" /> populated with values from <paramref name="signal" />.</returns>
    public static SignalEntity ToEntity(this Signal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        return new SignalEntity
        {
                Id = signal.Id,
                SignalId = signal.SignalId,
                SignalText = signal.SignalText,
                Source = signal.Source,
                Timestamp = signal.Timestamp
        };
    }
}