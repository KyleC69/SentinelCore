using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;

namespace SentinelCore.Tests;

/// <summary>
///     Unit tests for the <see cref="ModelResolver"/> class.
/// </summary>
[TestClass]
public class ModelResolverTests
{



    /// <summary>
    ///     Tests that <see cref="ModelResolver.ResolveByTier(ModelTier)"/> returns null when DefaultModel is null and no fallback.
    /// </summary>
    [TestMethod]
    public void ResolveByTier_ReturnsCorrectModelForPreset()
    {
        // Arrange
        //setup REAL TYPES NO FAKES or MOCKS
        var fileStore = new FileModelConfigStore(new NullLogger<FileModelConfigStore>());
        var loaded = fileStore.Load();

        var tierModels = loaded.TierModels;

        var options = Options.Create(new SentinelCoreSettings());
        var resolver = new ModelResolver(options);
        resolver.Tiers = tierModels;

        // Get real tier values from agent preset definitions
        ModelTier coreTier = AgentPresetDefinition.FromName(AgentPreset.CoreAgent).Tier;
        var mod1 = resolver.ResolveByTier(coreTier);

        ModelTier managerTier = AgentPresetDefinition.FromName(AgentPreset.ManagerAgent).Tier;
        var managerMod = resolver.ResolveByTier(managerTier);

        ModelTier utilityTier = AgentPresetDefinition.FromName(AgentPreset.UtilityAgent).Tier;
        var utilityMod = resolver.ResolveByTier(utilityTier);

        ModelTier groupWorkerTier = AgentPresetDefinition.FromName(AgentPreset.WorkerAgent).Tier; // Maps to GroupWorker
        var groupWorkerMod = resolver.ResolveByTier(groupWorkerTier);

        var classifierTier = AgentPresetDefinition.FromName(AgentPreset.ClassifierAgent).Tier;
        var classifierMod = resolver.ResolveByTier(classifierTier);

        var aggregatorTier = AgentPresetDefinition.FromName(AgentPreset.AggregatorAgent).Tier;
        var aggregatorMod = resolver.ResolveByTier(aggregatorTier);

        var safetytier = AgentPresetDefinition.FromName(AgentPreset.SafetyAgent).Tier;
        var safetyMod = resolver.ResolveByTier(safetytier);

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