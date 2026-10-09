// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         WorkflowOutputEventArgs.cs
// Author: Kyle L. Crowder
// Build Num:  100908



namespace SentinelCore.Contracts.Events;





public sealed class WorkflowOutputEventArgs
{

    public WorkflowOutputEventArgs(object message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }








    public object Message { get; set; }
}