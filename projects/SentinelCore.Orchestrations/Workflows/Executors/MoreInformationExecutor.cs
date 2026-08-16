// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MoreInformationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Workflows.Executors;





public class MoreInformationExecutor() : Executor<string, string>("MoreInformationStep")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return ValueTask.FromResult(message);
    }
}