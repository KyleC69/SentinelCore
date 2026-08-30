// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CreateCaseViewModel.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using SentinelCore.Cfe;
using SentinelCore.UI.Services;




namespace SentinelCore.UI.ViewModels;


/// <summary>
///     View-model for the Create Case page.
///     Allows users to create a new case by providing a signal description.
/// </summary>
public sealed partial class CreateCaseViewModel : ObservableObject, INavigationAware
{
    private readonly ICaseFlowEngine _caseFlowEngine;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private bool _isBusy;

    private readonly ILogger<CreateCaseViewModel> _logger;

    [ObservableProperty] private string _resultMessage = string.Empty;

    [ObservableProperty] private bool _showResult;

    [ObservableProperty] private string _signalSource = string.Empty;





    /// <summary>
    ///     Creates a new <see cref="CreateCaseViewModel" /> with required dependencies.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine for creating cases.</param>
    /// <param name="logger">The logger for this view-model.</param>
    public CreateCaseViewModel(ICaseFlowEngine caseFlowEngine, ILogger<CreateCaseViewModel> logger)
    {
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }





    public void OnNavigatedFrom()
    {
    }





    public void OnNavigatedTo(object? parameter)
    {
        // Reset form on navigation
        Description = string.Empty;
        SignalSource = string.Empty;
        ResultMessage = string.Empty;
        ShowResult = false;
    }





    private bool CanCreateCase() => !IsBusy && !string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(SignalSource);





    [RelayCommand(CanExecute = nameof(CanCreateCase))]
    private async Task CreateCaseAsync()
    {
        IsBusy = true;
        ShowResult = false;

        try
        {
            Signal signal = new(Description, SignalSource);

            Guid caseId = await _caseFlowEngine.CreateCaseAsync(signal);

            ResultMessage = $"Case created successfully. Case ID: {caseId}";
            ShowResult = true;

            _logger.LogInformation("Created case {CaseId} via UI", caseId);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Error creating case: {ex.Message}";
            ShowResult = true;
            _logger.LogError(ex, "Failed to create case via UI");
        }
        finally
        {
            IsBusy = false;
            CreateCaseCommand.NotifyCanExecuteChanged();
        }
    }





    [RelayCommand]
    private void ResetForm()
    {
        Description = string.Empty;
        SignalSource = string.Empty;
        ResultMessage = string.Empty;
        ShowResult = false;
    }
}