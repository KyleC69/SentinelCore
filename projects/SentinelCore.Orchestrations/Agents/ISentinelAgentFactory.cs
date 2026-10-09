// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ISentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  100815



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Defines the contract for building <see cref="AIAgent" /> instances from agent presets.
/// </summary>
public interface ISentinelAgentFactory
{

    /// <summary>
    ///     Builds an <see cref="AIAgent" /> instance from the specified profile.
    ///     <para>
    ///         This method is for callers that need to construct an agent from a pre-built
    ///         Prefer <see /> for preset-based construction.
    ///     </para>
    /// </summary>
    /// <param name="preset"></param>
    /// <param name="profile">The agent profile containing configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <see cref="AIAgent" /> instance.</returns>
    Task<AIAgent> BuildFromProfileAsync(AgentPresetDefinition preset, AgentProfile profile, CancellationToken cancellationToken = default);








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
    Task<AIAgent> CreateAgentAsync(string presetName, CancellationToken cancellationToken = default);








    Task<AIAgent> CreateAgentAsync(string presetName, string classifierInstruction, ChatResponseFormatJson forJsonSchema, CancellationToken none);
}
