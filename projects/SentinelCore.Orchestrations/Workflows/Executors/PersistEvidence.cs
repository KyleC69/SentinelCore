// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PersistEvidence.cs
// Author: Kyle L. Crowder
// Build Num:  091200



namespace SentinelCore.Orchestrations.Workflows.Executors;





public sealed class PersistEvidence() : Executor<string, string>("PersistEvidence")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}