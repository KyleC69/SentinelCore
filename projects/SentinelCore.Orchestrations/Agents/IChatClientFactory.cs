// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IChatClientFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Contracts.Contracts;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Defines the contract for creating <see cref="IChatClient" /> instances
///     from model profiles.
/// </summary>
public interface IChatClientFactory
{
    /// <summary>
    ///     Creates an <see cref="IChatClient" /> based on the model profile.
    ///     Returns the raw provider client without SentinelCore middleware wrappers.
    /// </summary>
    /// <param name="model">The model profile containing provider, endpoint, and credentials.</param>
    /// <returns>A configured <see cref="IChatClient" /> for the specified provider.</returns>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the provider specified in <paramref name="model" /> is not supported.
    /// </exception>
    IChatClient CreateChatClient(ModelProfile model);
}