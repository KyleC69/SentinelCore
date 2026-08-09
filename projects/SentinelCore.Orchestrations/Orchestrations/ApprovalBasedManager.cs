// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ApprovalBasedManager.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Orchestrations;





public class ApprovalBasedManager : RoundRobinGroupChatManager
{
    private readonly string _approverName;








    public ApprovalBasedManager(IReadOnlyList<AIAgent> agents, string approverName) : base(agents)
    {
        _approverName = approverName;
    }








    // Override to add custom termination logic
    protected override ValueTask<bool> ShouldTerminateAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        ChatMessage? last = history.LastOrDefault();
        bool shouldTerminate = last?.AuthorName == _approverName && last.Text.Contains("approve", StringComparison.OrdinalIgnoreCase);

        return ValueTask.FromResult(shouldTerminate);
    }
}