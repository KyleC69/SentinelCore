using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.Orchestrations.Workflows.Executors;




[YieldsOutput(typeof(Case))]
public sealed partial class EvaluationExec : Executor
{

    /// <summary>Initialize the executor with a unique identifier</summary>
    /// <param name="id">A unique identifier for the executor.</param>
    /// <param name="options">Configuration options for the executor. If <c>null</c>, default options will be used.</param>
    /// <param name="declareCrossRunShareable">Declare that this executor may be used simultaneously by multiple runs safely.</param>
    public EvaluationExec(string id, ExecutorOptions? options = null, bool declareCrossRunShareable = false) : base(id, options, declareCrossRunShareable)
    {
    }








    [MessageHandler]
    public async ValueTask<Case> HandleCaseAsync(Case caseFile, IWorkflowContext context, CancellationToken cancellationToken)
    {





        await context.YieldOutputAsync(caseFile);
        return caseFile;
    }







}
