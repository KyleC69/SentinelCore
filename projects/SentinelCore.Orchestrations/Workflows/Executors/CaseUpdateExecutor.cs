// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseUpdateExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.CaseFlowEngine.Cfe;




namespace SentinelCore.Orchestrations.Workflows.Executors;





public class CaseUpdateExecutor(ICaseFlowEngine engine) : Executor<string, string>("CaseUpdateExec")
{
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {


        // ##########  DO SOME WORK
        throw new NotImplementedException();
    }
}