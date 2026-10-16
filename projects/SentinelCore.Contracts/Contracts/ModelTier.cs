// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ModelTier.cs
// Author: Kyle L. Crowder
// Build Num:  100909



namespace SentinelCore.Contracts.Contracts;





/// <summary>
///     Defines the model tier for an agent, used to select the default model
///     from <see cref="SentinelCoreSettings" />.
/// </summary>
public enum ModelTier
{
    /// <summary>
    ///     Core reasoning agent — uses the frontier/default model.
    /// </summary>
    Core,

    /// <summary>
    ///     Manager/orchestrator agent — uses the manager model.
    /// </summary>
    Manager,

    /// <summary>
    ///     Utility/worker agent — uses the utility model.
    /// </summary>
    Utility,
    Worker,
    Classifier
}