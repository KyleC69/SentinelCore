// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CriticalAlert.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.CaseFlowEngine.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





internal sealed class CriticalAlert(ICaseFlowEngine flowEngine) : Executor<SignalHypothesis, string>("CriticalError")
{

    public override ValueTask<string> HandleAsync(SignalHypothesis message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }
}