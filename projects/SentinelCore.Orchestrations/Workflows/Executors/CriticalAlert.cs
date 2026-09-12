// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CriticalAlert.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using SentinelCore.CaseFlowEngine.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





internal sealed class CriticalAlert(ICaseFlowEngine flowEngine) : Executor<string, string>("CriticalError")
{

    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        return default;
    }
}