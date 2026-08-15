// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CriticalAlert.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.Cfe;




namespace SentinelCore.Workflows.Executors;





internal sealed class CriticalAlert(ICaseFlowEngine flowEngine) : Executor<string, string>("CriticalError")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}