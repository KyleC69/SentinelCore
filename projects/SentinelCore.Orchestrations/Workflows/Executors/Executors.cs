// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         Executors.cs
// Author: Kyle L. Crowder
// Build Num:  091112



namespace SentinelCore.Orchestrations.Workflows.Executors;





public class PersistTask() : Executor<string, string>("PersistTask")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}