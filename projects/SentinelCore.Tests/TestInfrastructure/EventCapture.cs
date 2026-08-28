// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         EventCapture.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     A test double for <see cref="ISentinelCoreEvents" /> that records every
///     raised event in public lists for assertion.
/// </summary>
public sealed class EventCapture : ISentinelCoreEvents
{
    public List<(string Message, Exception Exception)> ErrorEvents { get; } = [];
    public List<OrchestrationActivityArgs> OrchestrationEvents { get; } = [];
    public List<SentinelOutputEventArgs> SentinelOutputEvents { get; } = [];

    public event Action<string, Exception>? ErrorOccurred;
    public event Action<OrchestrationActivityArgs>? OrchestrationEvent;








    public void RaiseError(string message, Exception exception)
    {
        ErrorEvents.Add((message, exception));
        ErrorOccurred?.Invoke(message, exception);
    }








    public void RaiseOrchestrationEvent(OrchestrationActivityArgs payload)
    {
        OrchestrationEvents.Add(payload);
        OrchestrationEvent?.Invoke(payload);
    }








    public void RaiseSentinelOutputEvent(SentinelOutputEventArgs payload)
    {
        SentinelOutputEvents.Add(payload);
        SentinelOutputEvent?.Invoke(payload);
    }








    public event Action<SentinelOutputEventArgs>? SentinelOutputEvent;








    /// <summary>
    ///     Clears all captured events (useful between sub-tests).
    /// </summary>
    public void Clear()
    {
        SentinelOutputEvents.Clear();
        OrchestrationEvents.Clear();
        ErrorEvents.Clear();
    }
}