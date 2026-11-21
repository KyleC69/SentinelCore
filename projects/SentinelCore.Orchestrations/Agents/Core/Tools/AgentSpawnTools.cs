// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentSpawnTools.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;





namespace SentinelCore.Orchestrations.Agents.Core.Tools;








/// <summary>
///     Provides the <c>sc_internal_spawn_agent</c> tool: spawns a one-shot utility agent from the
///     <see cref="AgentPreset.UtilityAgent" /> preset, runs a single task with an explicitly requested toolset,
///     and returns the agent's answer to the calling agent.
/// </summary>
/// <remarks>
///     <para>
///         The spawned agent is transient: it lives for exactly one task, is not retained, registered or reused,
///         and its result is returned to the caller verbatim. The tool exists so an agent can dispatch an ad-hoc
///         task to a fresh utility agent without creating an investigation workflow.
///     </para>
///     <para>
///         The requested tool groups replace the UtilityAgent preset's default groups — a tool selection replaces
///         the preset default, it never unions with it — so the spawned agent receives exactly the requested
///         toolset.
///     </para>
///     <para>
///         The agent factory is resolved lazily from the service provider inside the tool method to avoid a
///         construction cycle: the factory depends on the registered <see cref="AITool" /> instances, one of which
///         is this tool.
///     </para>
/// </remarks>
public sealed class AgentSpawnTools
{
    /// <summary>
    ///     Instructions appended to the spawned agent's preset instructions that scope it to its one-shot task.
    /// </summary>
    private const string CurationInstructions =
            "You are a transient utility agent spawned for exactly one task. Complete the task and return a concise, self-contained answer that the requesting agent can use directly. You will not be retained after this task; do not ask follow-up questions.";

    private readonly IServiceProvider _services;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentSpawnTools" /> class.
    /// </summary>
    /// <param name="services">The service provider used to resolve the agent factory lazily at invocation time.</param>
    public AgentSpawnTools(IServiceProvider services)
    {
        _services = services;
    }








    /// <summary>
    ///     Spawns a one-shot utility agent with the requested tool groups, runs the supplied task and returns the
    ///     agent's answer.
    /// </summary>
    /// <param name="task">The task for the spawned agent to perform.</param>
    /// <param name="groups">The registered Sentinel Core tool group names the spawned agent may use.</param>
    /// <param name="cancellationToken">A token to cancel the spawn and the task run.</param>
    /// <returns>A success result carrying the spawned agent's answer, or a failure result describing the error.</returns>
    /// <remarks>
    ///     Cancellation is not treated as a tool failure: <see cref="OperationCanceledException" /> propagates to the
    ///     caller. Every other exception is reported as a failure result so the calling agent receives a
    ///     <see cref="ToolResult" /> as mandated by the tooling contract.
    /// </remarks>
    [Description("Spawns a one-shot utility agent with the requested tool groups, runs the supplied task and returns the agent's answer. Group names must be registered Sentinel Core tool group names.")]
    public async Task<ToolResult> SpawnAgent(
            [Description("The task for the spawned agent to perform")] string task,
            [Description("The tool group names the spawned agent may use")] string[] groups,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            return ToolResult.Fail("The task must not be null, empty, or whitespace.");
        }

        if (groups is null || groups.Length == 0)
        {
            return ToolResult.Fail($"At least one tool group name is required. Valid group names: {string.Join(", ", AgentToolGroupCatalog.Names)}.");
        }

        try
        {
            ISentinelAgentFactory factory = _services.GetRequiredService<ISentinelAgentFactory>();
            string agentName = $"UtilityAgent-{Guid.NewGuid().ToString("N")[..8]}";

            AIAgent worker = await factory.CreateAgentAsync(
                    AgentPreset.UtilityAgent,
                    agentName,
                    "Transient utility agent spawned for a single ad-hoc task; not retained after the task completes.",
                    CurationInstructions,
                    AgentToolSelection.FromGroups(groups)).ConfigureAwait(false);

            AgentResponse response = await worker.RunAsync(task, cancellationToken: cancellationToken).ConfigureAwait(false);

            return ToolResult.Ok(response.Text ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            return ToolResult.Fail($"Failed to spawn or run the utility agent. Exception: {e.Message}");
        }
    }
}
