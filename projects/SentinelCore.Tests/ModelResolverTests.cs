// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ModelResolverTests.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for the <see cref="ModelResolver" /> class.
/// </summary>
[TestClass]
public class ModelResolverTests
{



    /// <summary>
    ///     Tests that <see cref="ModelResolver.ResolveByTier(ModelTier)" /> returns null when DefaultModel is null and no
    ///     fallback.
    /// </summary>
    [TestMethod]
    public void ResolveByTier_ReturnsCorrectModelForPreset()
    {
        // Arrange
        //setup REAL TYPES NO FAKES or MOCKS
        FileModelConfigStore fileStore = new(new NullLogger<FileModelConfigStore>());
        ModelConfigDocument? loaded = fileStore.Load();

        IDictionary<ModelTier, ModelProfile> tierModels = loaded.TierModels;

        IOptions<SentinelCoreSettings> options = Options.Create(new SentinelCoreSettings());
        ModelResolver resolver = new(options);
        resolver.Tiers = tierModels;

        // Get real tier values from agent preset definitions
        ModelTier coreTier = AgentPresetDefinition.FromName(AgentPreset.CoreAgent).Tier;
        ModelProfile? mod1 = resolver.ResolveByTier(coreTier);

        ModelTier managerTier = AgentPresetDefinition.FromName(AgentPreset.ManagerAgent).Tier;
        ModelProfile? managerMod = resolver.ResolveByTier(managerTier);

        ModelTier utilityTier = AgentPresetDefinition.FromName(AgentPreset.UtilityAgent).Tier;
        ModelProfile? utilityMod = resolver.ResolveByTier(utilityTier);

        ModelTier groupWorkerTier = AgentPresetDefinition.FromName(AgentPreset.WorkerAgent).Tier; // Maps to GroupWorker
        ModelProfile? groupWorkerMod = resolver.ResolveByTier(groupWorkerTier);

        ModelTier classifierTier = AgentPresetDefinition.FromName(AgentPreset.ClassifierAgent).Tier;
        ModelProfile? classifierMod = resolver.ResolveByTier(classifierTier);

        ModelTier aggregatorTier = AgentPresetDefinition.FromName(AgentPreset.AggregatorAgent).Tier;
        ModelProfile? aggregatorMod = resolver.ResolveByTier(aggregatorTier);

        ModelTier safetytier = AgentPresetDefinition.FromName(AgentPreset.SafetyAgent).Tier;
        ModelProfile? safetyMod = resolver.ResolveByTier(safetytier);

        // Act & Assert
        // All tiers should return null because there is no model to fall back to.
        Assert.IsNotNull(resolver.ResolveByTier(coreTier));
        Assert.IsNotNull(resolver.ResolveByTier(managerTier));
        Assert.IsNotNull(resolver.ResolveByTier(utilityTier));
        Assert.IsNotNull(resolver.ResolveByTier(groupWorkerTier));
        Assert.IsNotNull(resolver.ResolveByTier(classifierTier));
        Assert.IsNotNull(resolver.ResolveByTier(aggregatorTier));
        Assert.IsNotNull(resolver.ResolveByTier(safetytier));
    }
}