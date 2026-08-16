// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseUpdateExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Cfe;




namespace SentinelCore.Workflows.Executors;





public class CaseUpdateExecutor(ICaseFlowEngine engine) : Executor<string, string>("CaseUpdateExec")
{
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}