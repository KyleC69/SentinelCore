// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AggregationExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Text;

using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executor that runs the Aggregator agent to collect and synthesize
///     investigation results.
/// </summary>
public class AggregationExecutor(ISystemReporter reporter) : Executor<ChatMessage, ChatMessage>("Aggregator")
{

    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellation)
    {
        StringBuilder responseBuilder = new();

        reporter.ReportInfo(message.Text);

        return message;
    }
}