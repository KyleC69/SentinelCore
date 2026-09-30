// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DetectionBoolResult.cs
// Author: Kyle L. Crowder
// Build Num:  092603



namespace SentinelCore.Orchestrations.Workflows.Helpers;





public sealed class DetectionBoolResult
{

    public DetectionBoolResult(ChatMessage? message, bool b)
    {
        IsTrue = b;
        Prompt = message ?? new ChatMessage(ChatRole.Assistant, "Default fallback message. Failure in workflow conditional results");
    }








    public bool IsTrue { get; init; }

    public ChatMessage Prompt { get; set; } = new();
}