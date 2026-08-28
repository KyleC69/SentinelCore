// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PersistEvidence.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Workflows.Executors.SentinelCore.Workflows.Executors;





public sealed class PersistEvidence() : Executor<string, string>("PersistEvidence")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}