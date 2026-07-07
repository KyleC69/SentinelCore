// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ISentinelEvents.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCore.Contracts;





public interface ISentinelEvents
{
    //   event EventHandler<ReasoningTraceEntry> ReasoningTrace;
    event EventHandler<AgentActivityLogEntry> AgentActivity;
    event EventHandler<WorkflowEventEntry> WorkflowEvent;
}