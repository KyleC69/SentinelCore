// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ModelConfigViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;




namespace SentinelCore.UI.ViewModels;





/// <summary>
///     View-model for the Model Configuration page. Owns one configuration card
///     per catalog agent (provider, model, endpoint, tuning). Saving persists the
///     document and applies it to the live <see cref="SentinelCoreSettings" /> so
///     the next agent build uses the new values. Agents without configuration
///     are gated at the factory — there is no fallback.
/// </summary>
public sealed partial class ModelConfigViewModel : ObservableObject, INavigationAware
{

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private ObservableCollection<AgentModelCard> _cards = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    private readonly ILogger<ModelConfigViewModel> _logger;

    [ObservableProperty] private string _resultMessage = string.Empty;

    private readonly SentinelCoreSettings _settings;

    [ObservableProperty] private bool _showResult;

    private readonly IModelConfigStore _store;








    /// <summary>
    ///     Creates the view-model and loads a card for every catalog agent.
    /// </summary>
    /// <param name="store">The persistence store for the configuration document.</param>
    /// <param name="settingsOptions">The live SentinelCore settings.</param>
    /// <param name="logger">The logger for this view-model.</param>
    public ModelConfigViewModel(IModelConfigStore store, IOptions<SentinelCoreSettings> settingsOptions, ILogger<ModelConfigViewModel> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = (settingsOptions ?? throw new ArgumentNullException(nameof(settingsOptions))).Value;
    }









    /// <summary>
    ///     Gets the supported model providers for the provider combo boxes.
    /// </summary>
    public IReadOnlyList<ModelProfile.ModelProvider> Providers { get; } = Enum.GetValues<ModelProfile.ModelProvider>().ToList();








    public void OnNavigatedFrom()
    {
    }








    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadAsync();
    }








    private bool CanSave() => !IsBusy && Cards.Count > 0;








    /// <summary>
    ///     Loads a card for every catalog agent, pre-populated from the live settings.
    /// </summary>
    private async Task LoadAsync()
    {
        IsBusy = true;

        try
        {
            List<AgentModelCard> cards = new();
            foreach (KeyValuePair<ModelTier, ModelProfile> entry in _settings.TierModels)
            {
                _settings.TierModels.TryGetValue(entry.Key, out ModelProfile? profile);
                cards.Add(new AgentModelCard(entry.Key, profile));
            }

            Cards = new ObservableCollection<AgentModelCard>(cards);
            _logger.LogTrace("Loaded {Count} agent model cards.", cards.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent model cards.");
            ResultMessage = $"Error loading configuration: {ex.Message}";
            ShowResult = true;
        }
        finally
        {
            IsBusy = false;
        }
    }








    /// <summary>
    ///     Persists every complete card to the store and applies them to the live
    ///     settings so the next agent build uses the new values. Incomplete cards
    ///     are reported — those agents remain gated.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ShowResult = false;

        try
        {
            ModelConfigDocument document = new();

            List<string> incomplete = new();
            foreach (AgentModelCard card in Cards)
            {
                ModelProfile? profile = card.ToModelProfile();

                if (profile is null)
                {
                    incomplete.Add(card.AgentName);
                    continue;
                }

                document.TierModels[card.Tier] = profile;
            }

            _store.Save(document);

            // Apply to the live settings — agents are rebuilt per orchestration,
            // so the next run picks these up.
            _settings.TierModels.Clear();
            foreach (KeyValuePair<ModelTier, ModelProfile> entry in document.TierModels)
            {
                _settings.TierModels[entry.Key] = entry.Value;
            }

            if (incomplete.Count > 0)
            {
                ResultMessage = $"Saved. Unconfigured (features using these agents stay gated): {string.Join(", ", incomplete)}";
            }
            else
            {
                ResultMessage = "All agent models saved.";
            }

            ShowResult = true;
            _logger.LogInformation("Saved model configuration for {Count} agents ({Incomplete} incomplete).", document.TierModels.Count, incomplete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save model configuration.");
            ResultMessage = $"Error saving configuration: {ex.Message}";
            ShowResult = true;
        }
        finally
        {
            IsBusy = false;

            // Keep the result visible for at least 3 seconds so the user can read it.
            _ = Task.Delay(3000).ContinueWith(_ => ShowResult = false);
        }
    }
}