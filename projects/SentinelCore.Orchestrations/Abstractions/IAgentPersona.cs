// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IAgentPersona.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Abstractions;





/// <summary>
///     Defines the persona for an agent, including its name, description, and instructions.
/// </summary>
public interface IAgentPersona
{

    /// <summary>
    ///     Gets a brief description of the agent's role.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Gets the system instructions that guide the agent's behavior.
    /// </summary>
    string Instructions { get; }

    /// <summary>
    ///     Gets the agent name.
    /// </summary>
    string Name { get; }
}