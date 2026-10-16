// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ISentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Defines the contract for building <see cref="AIAgent" /> instances from agent presets.
/// </summary>
public interface ISentinelAgentFactory
{



    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> based on the specified preset name.
    ///     The agent is constructed with persistent infrastructure (chat client, wrappers, context providers, tools).
    ///     Instructions and response format are per-call concerns handled by the executor at invocation time,
    ///     not baked into the agent at construction time.
    /// </summary>
    /// <param name="presetName">
    ///     The name of the preset to use for creating the agent (e.g., "Classifier", "TheCore").
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe while waiting for the task to complete, enabling cancellation of the operation.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the specified <paramref name="presetName" /> does not correspond to any registered preset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default);








    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string taskInstructions, CancellationToken cancellationToken = default);



    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string taskInstructions, ChatResponseFormatJson forJsonSchema, CancellationToken none);


    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions);


    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription);
}