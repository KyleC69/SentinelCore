// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InstructionLayerBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  092200



#nullable enable

using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Builds layered <see cref="ChatMessages" /> for agent invocation.
///     Instructions are assembled at the call site (executor), not baked into
///     the agent at construction time. This allows the same agent instance to be
///     reused with different instructions per invocation.
///     <para>
///         The standard instruction stack has three layers:
///         <list type="number">
///             <item><b>Platform domain</b> (always present) — grounds the agent in the system's domain.</item>
///             <item><b>Preset/role instructions</b> — the agent's default role instructions from its preset.</item>
///             <item><b>Task instructions</b> (optional) — executor-specific instructions for the current task.</item>
///         </list>
///     </para>
///     <para>
///         Executors should use this builder to assemble instructions rather than
///         constructing <see cref="ChatMessages" /> directly. This ensures consistent
///         layering and makes instruction composition testable.
///     </para>
/// </summary>
public sealed class InstructionLayerBuilder
{
    private readonly string _platformDomain;





    /// <summary>
    ///     Initializes a new instance of the <see cref="InstructionLayerBuilder" /> class.
    /// </summary>
    /// <param name="platformDomain">
    ///     The platform domain instruction that grounds every agent in the system's domain.
    ///     This is always Layer 1 in the instruction stack.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="platformDomain" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="platformDomain" /> is whitespace.</exception>
    public InstructionLayerBuilder(string platformDomain)
    {
        ArgumentNullException.ThrowIfNull(platformDomain);

        if (string.IsNullOrWhiteSpace(platformDomain))
        {
            throw new ArgumentException("Platform domain instructions must not be empty or whitespace.", nameof(platformDomain));
        }

        _platformDomain = platformDomain;
    }





    /// <summary>
    ///     Builds the standard 3-layer instruction stack.
    /// </summary>
    /// <param name="presetInstructions">
    ///     The preset/role instructions for the agent (Layer 2).
    ///     May be <c>null</c> or empty if the agent has no preset instructions.
    /// </param>
    /// <param name="taskInstructions">
    ///     Optional task-specific instructions for the current invocation (Layer 3).
    ///     May be <c>null</c> or empty if no task-specific instructions are needed.
    /// </param>
    /// <returns>
    ///     A <see cref="ChatMessages" /> instance containing the assembled system messages
    ///     in order: platform domain, preset instructions (if any), then task instructions (if any).
    /// </returns>
    public ChatMessages Build(string? presetInstructions = null, string? taskInstructions = null)
    {
        ChatMessages messages = new();

        // Layer 1: Platform domain (always present, grounds the agent)
        messages.AddSystemMessage(_platformDomain);

        // Layer 2: Preset/role instructions (from preset or override)
        if (!string.IsNullOrWhiteSpace(presetInstructions))
        {
            messages.AddSystemMessage(presetInstructions);
        }

        // Layer 3: Task-specific instructions (executor-specific)
        if (!string.IsNullOrWhiteSpace(taskInstructions))
        {
            messages.AddSystemMessage(taskInstructions);
        }

        return messages;
    }





    /// <summary>
    ///     Gets the platform domain instruction string used by this builder.
    ///     Useful for testing and diagnostics.
    /// </summary>
    public string PlatformDomain => _platformDomain;
}
