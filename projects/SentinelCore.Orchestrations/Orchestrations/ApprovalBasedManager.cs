// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ApprovalBasedManager.cs
// Author: Kyle L. Crowder
// Build Num:  100908



namespace SentinelCore.Orchestrations.Orchestrations;





public class ApprovalBasedManager : RoundRobinGroupChatManager
{
    private readonly string _approverName;








    public ApprovalBasedManager(IReadOnlyList<AIAgent> agents, string approverName) : base(agents)
    {
        if (agents == null) throw new ArgumentNullException(nameof(agents));
        if (string.IsNullOrWhiteSpace(approverName)) throw new ArgumentException("Approver name cannot be null or empty.", nameof(approverName));

        _approverName = approverName;
    }








    // Override to add custom termination logic
    protected override ValueTask<bool> ShouldTerminateAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        try
        {
            if (history == null || history.Count == 0) return ValueTask.FromResult(false);

            ChatMessage? last = history[^1];
            if (last == null) return ValueTask.FromResult(false);

            bool isApprover = string.Equals(last.AuthorName, _approverName, StringComparison.OrdinalIgnoreCase);
            bool hasApproved = last.Text?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false;

            return ValueTask.FromResult(isApprover && hasApproved);
        }
        catch (Exception)
        {
            // In orchestration, we prefer continuing the conversation over crashing the workflow on a termination check failure.
            return ValueTask.FromResult(false);
        }
    }
}