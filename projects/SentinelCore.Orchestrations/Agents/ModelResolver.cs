// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ModelResolver.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Resolves a <see cref="ModelProfile" /> for a logical agent name.
///     The logic mirrors the previous implementation in <see cref="AgentProfileBuilder" />
///     but is extracted to a dedicated service so it can be reused and unit‑tested.
/// </summary>
public interface IModelResolver
{







    /// <summary>
    ///     Resolves a model based on the <see cref="ModelTier" /> defined in an <see cref="AgentPresetDefinition" />.
    ///     This is the preferred path for new code – it selects the appropriate model from
    ///     <see cref="SentinelCoreSettings" /> according to the tier (Core, Manager, Utility, GroupWorker).
    /// </summary>
    ModelProfile? ResolveByTier(ModelTier tier);
}





/// <summary>
///     Default implementation of <see cref="IModelResolver" /> that reads from <see cref="SentinelCoreSettings" />.
/// </summary>
public sealed class ModelResolver : IModelResolver
{
    private readonly SentinelCoreSettings _options;








    public ModelResolver(IOptions<SentinelCoreSettings> options)
    {
        Throw.IfNull(options);
        _options = options.Value;
    }














    public ModelProfile? ResolveByTier(ModelTier tier)
    {
        // Map tier to the appropriate model configuration from settings.
        return tier switch
        {
            ModelTier.Core => _options.DefaultModel,
            ModelTier.Manager => _options.ManagerModel ?? _options.DefaultModel,
            ModelTier.Utility => _options.DefaultUtilityModel ?? _options.DefaultModel,
            ModelTier.GroupWorker => _options.DefaultUtilityModel ?? _options.DefaultModel,
            _ => _options.DefaultModel
        };
    }
}