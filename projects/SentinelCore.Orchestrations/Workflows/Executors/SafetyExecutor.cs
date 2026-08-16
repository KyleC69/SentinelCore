// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Abstractions;
using SentinelCore.Agents;




namespace SentinelCore.Workflows.Executors;





public sealed class SafetyExecutor(ISystemReporter reporter, ISentinelAgentFactory factory) : Executor<ChatMessage, ChatMessage>("SafetyExecutor")
{

    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        reporter.ReportInfo("Starting Safety filter");




        reporter.ReportInfo("Leaving safety exec");
        return message;
    }
}