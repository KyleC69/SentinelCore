using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;




[YieldsOutput(typeof(string))]
public partial class TerminateWorkflow : Executor
{
    private ISystemReporter _reporter;

    public TerminateWorkflow(ISystemReporter reporter) : base("TerminateWorkflow")
    {
        _reporter = reporter;
    }






    /// <summary>
    /// Gracefully exit workflow and return any final messages.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [MessageHandler]
    private async ValueTask HandleTerminationAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {
        try
        {


// Temporary status msg to track workflow exit
            await context.YieldOutputAsync("Workflow terminated gracefully.", token);


        }
        catch (OperationCanceledException e)
        {
           _reporter.ReportWarning("Workflow cancelled by user");

        }
    }


}
