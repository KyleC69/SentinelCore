// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Signal.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Cfe;





public sealed class Signal
{
    public Signal(string text, string source)
    {
        SignalText = text;
        Source = source;
        Timestamp = DateTime.Now;
        // Initialize non-nullable Notes to an empty string to satisfy CS8618.
        Notes = string.Empty;
    }








    public int Id { get; set; }
    public string Notes { get; set; }
    public int SignalId { get; set; }

    public string SignalText { get; set; }

    public string Source { get; set; }

    public DateTime Timestamp { get; set; }
}