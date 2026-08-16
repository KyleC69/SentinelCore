// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SubWorkflowExec.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Abstractions;




namespace SentinelCore.Workflows.Executors;





public class SubWorkflowExec(Workflow flow, ISystemReporter reporter) : Executor<ChatMessage, ChatMessage>("Subflowexec")
{
    // TODO: Implement SubWorkflowExec logic








    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = new())
    {

        Run result = await InProcessExecution.RunAsync(flow, message);


        foreach (WorkflowEvent evt in result.NewEvents)
            if (evt is WorkflowOutputEvent outputEvt)
            {
                Console.WriteLine($"Final result: {outputEvt.Data}");
                return new ChatMessage(ChatRole.User, outputEvt?.Data?.ToString() ?? "");
            }

        return new ChatMessage();
    }
}