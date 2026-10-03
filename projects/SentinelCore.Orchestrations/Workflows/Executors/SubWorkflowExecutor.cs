// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SubWorkflowExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





[YieldsOutput(typeof(ChatMessage))]
public sealed partial class SubWorkflowExecutor : Executor
{
    private readonly Workflow _evidence;








    public SubWorkflowExecutor(Workflow evidenceGather) : base("SubWorkflowExecutor")
    {
        Name = Id;
        _evidence = evidenceGather ?? Throw.IfNull(evidenceGather);
    }








    public string Name { get; }








    [MessageHandler]
    public async ValueTask<ChatMessage> HandleMessage(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        // Implement the logic for handling the ChatMessage here
        // For example, you can log the message or perform some processing

        Run response = await InProcessExecution.RunAsync(_evidence, message);



        return message;
    }
}