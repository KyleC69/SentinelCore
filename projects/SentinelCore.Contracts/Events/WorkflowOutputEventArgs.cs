// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         WorkflowOutputEventArgs.cs
// Author: Kyle L. Crowder
// Build Num:  100519



using Microsoft.Extensions.AI;




namespace SentinelCore.Contracts.Events;





public class WorkflowOutputEventArgs
{

    public WorkflowOutputEventArgs(ChatMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public ChatMessage Message { get; set; }
}