// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         IQuarantineStore.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.AI;




namespace SentinelCore.Security.Quarantine;





/// <summary>
///     Persists messages that failed provenance validation to the secure quarantine
///     folder, together with the reason. Consumers (operator UI quarantine browser)
///     subscribe to <see cref="RecordQuarantined" /> to observe quarantine events as
///     they happen.
/// </summary>
public interface IQuarantineStore
{



    /// <summary>Gets the quarantine records persisted in the secure folder, oldest first.</summary>
    IReadOnlyList<QuarantineRecord> Records { get; }








    /// <summary>
    ///     Quarantines the message: persists a snapshot of its signed content to the
    ///     secure quarantine folder and logs the event details.
    /// </summary>
    /// <param name="message">The offending message, stored unchanged as a snapshot.</param>
    /// <param name="reason">The validation failure that triggered quarantine.</param>
    /// <param name="detail">Human-readable details of the event.</param>
    /// <returns>The created quarantine record.</returns>
    QuarantineRecord Quarantine(ChatMessage message, ProvenanceFailure reason, string detail);








    /// <summary>Raised immediately after a message is quarantined.</summary>
    event Action<QuarantineRecord>? RecordQuarantined;
}