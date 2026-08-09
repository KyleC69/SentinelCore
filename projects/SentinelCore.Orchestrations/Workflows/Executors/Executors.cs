// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         Executors.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Workflows.Executors;





public class PersistTask() : Executor<string, string>("PersistTask")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}