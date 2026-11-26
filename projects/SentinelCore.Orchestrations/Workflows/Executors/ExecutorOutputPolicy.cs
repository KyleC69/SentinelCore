// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorOutputPolicy.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Central output policy for SentinelCore workflow executors.
/// </summary>
/// <remarks>
///     Only messages a SentinelCore executor yields explicitly via
///     <see cref="IWorkflowContext.YieldOutputAsync" /> are intended to reach the UI. MAF otherwise
///     auto-yields every non-void message-handler return value as a
///     <see cref="WorkflowOutputEvent" />, which floods the chat with internal plumbing payloads.
///     Suppressing that behavior requires per-executor options, but <see cref="ExecutorOptions" />
///     has an internal constructor and its only public instance (<see cref="ExecutorOptions.Default" />)
///     is a shared process-wide singleton, so mutation is not an option. An isolated instance is
///     therefore obtained through the public derived type <see cref="StatefulExecutorOptions" />,
///     which is inert for stateless executors because <see cref="Executor" /> only consults the
///     auto-send and auto-yield flags. If a future MAF release opens the
///     <see cref="ExecutorOptions" /> constructor, swap the implementation here.
/// </remarks>
internal static class ExecutorOutputPolicy
{
    /// <summary>
    ///     Creates isolated executor options that suppress MAF's automatic yield of message-handler
    ///     return values so that only explicit <c>YieldOutputAsync</c> calls surface as workflow
    ///     output for the UI. Handler return values are still sent to connected executors
    ///     (<see cref="ExecutorOptions.AutoSendMessageHandlerResultObject" /> remains enabled), so
    ///     in-graph message routing is unaffected. Each executor must declare its explicitly yielded
    ///     payload types with <see cref="YieldsOutputAttribute" /> or the workflow run will reject
    ///     the yield at runtime.
    /// </summary>
    /// <returns>A new options instance with handler-result auto-yield disabled.</returns>
    public static ExecutorOptions CreateExplicitYieldsOnly() => new StatefulExecutorOptions { AutoYieldOutputHandlerResultObject = false };
}