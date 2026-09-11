// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         AgentModelCard.cs
// Author: Kyle L. Crowler
// Build Num:  091003



using CommunityToolkit.Mvvm.ComponentModel;

using SentinelCore.Contracts;





namespace SentinelCore.UI.Models;





/// <summary>
///     One card on the Model Configuration page: the provider/model/tuning
///     configuration for a single logical agent from the agent catalog.
/// </summary>
public sealed partial class AgentModelCard : ObservableObject
{
    /// <summary>
    ///     The logical agent name this card configures.
    /// </summary>
    public string AgentName { get; }

    /// <summary>
    ///     The role tier used when no per-agent model is configured.
    /// </summary>
    public SentinelCore.Contracts.ModelProfile.ModelProvider? FallbackProvider { get; }

    /// <summary>
    ///     Backing field for <see cref="ApiKey" />.
    /// </summary>
    [ObservableProperty]
    private string? _apiKey;

    /// <summary>
    ///     Backing field for <see cref="Endpoint" />.
    /// </summary>
    [ObservableProperty]
    private string _endpoint = string.Empty;

    /// <summary>
    ///     Backing field for <see cref="IsConfigured" />.
    /// </summary>
    [ObservableProperty]
    private bool _isConfigured;

    /// <summary>
    ///     Backing field for <see cref="MaxOutputTokens" />.
    /// </summary>
    [ObservableProperty]
    private int _maxOutputTokens = 16000;

    /// <summary>
    ///     Backing field for <see cref="ModelId" />.
    /// </summary>
    [ObservableProperty]
    private string _modelId = string.Empty;

    /// <summary>
    ///     Backing field for <see cref="Provider" />.
    /// </summary>
    [ObservableProperty]
    private SentinelCore.Contracts.ModelProfile.ModelProvider _provider = SentinelCore.Contracts.ModelProfile.ModelProvider.Ollama;

    /// <summary>
    ///     Backing field for <see cref="Temperature" />.
    /// </summary>
    [ObservableProperty]
    private float _temperature = 0.1f;

    /// <summary>
    ///     Backing field for <see cref="TopK" />.
    /// </summary>
    [ObservableProperty]
    private int _topK = 1;

    /// <summary>
    ///     Backing field for <see cref="TopP" />.
    /// </summary>
    [ObservableProperty]
    private float _topP = 0.1f;





    /// <summary>
    ///     Creates a card for the given logical agent name.
    /// </summary>
    /// <param name="agentName">The logical agent name from the catalog.</param>
    public AgentModelCard(string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        AgentName = agentName;
    }





    /// <summary>
    ///     Creates a card pre-populated from an existing model profile.
    /// </summary>
    /// <param name="agentName">The logical agent name from the catalog.</param>
    /// <param name="profile">The configured model profile, or <c>null</c> for an unconfigured card.</param>
    public AgentModelCard(string agentName, ModelProfile? profile)
        : this(agentName)
    {
        if (profile is null)
        {
            return;
        }

        Provider = profile.Provider;
        Endpoint = profile.Endpoint ?? string.Empty;
        ModelId = profile.ModelId ?? string.Empty;
        Temperature = profile.Temperature;
        MaxOutputTokens = profile.MaxOutputTokens ?? 16000;
        TopK = profile.TopK;
        TopP = profile.TopP;
        ApiKey = profile.ApiKey;
        IsConfigured = true;
    }





    /// <summary>
    ///     Builds a <see cref="ModelProfile" /> from the card's current values.
    /// </summary>
    /// <returns>The model profile, or <c>null</c> when the card is not complete.</returns>
    public ModelProfile? ToModelProfile()
    {
        if (string.IsNullOrWhiteSpace(Endpoint) || string.IsNullOrWhiteSpace(ModelId))
        {
            return null;
        }

        return new ModelProfile(
            Endpoint.Trim(),
            ModelId.Trim(),
            Temperature,
            MaxOutputTokens,
            topK: TopK,
            topP: TopP,
            provider: Provider,
            apiKey: string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey);
    }





    /// <summary>
    ///     Gets a value indicating whether the card has the minimum fields
    ///     required to build a model profile.
    /// </summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ModelId);
}
