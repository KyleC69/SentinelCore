// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DirectAnswerExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Orchestrations.Agents;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Executor that runs TheCore for direct answers (CanAnswerDirectly, PatternMatch,
///     IsNoise, MoreInformationRequired routes).
///     May or may not be used - hold for decision
/// </summary>
public sealed class DirectAnswerExecutor(ICaseGenerator caseGenerator) : Executor<string, string>("DirectAnswer")
{

    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken)
    {






        return message;

    }
}