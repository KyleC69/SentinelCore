// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CreateCaseViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using SentinelCore.Cfe;
using SentinelCore.Contracts;
using SentinelCoreAdmin.Contracts.ViewModels;




namespace SentinelCoreAdmin.ViewModels;


/// <summary>
///     View-model for the Create Case page.
///     Allows users to create a new case by providing a signal description.
/// </summary>
public partial class CreateCaseViewModel : ObservableObject, INavigationAware
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ILogger<CreateCaseViewModel> _logger;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _resultMessage = string.Empty;

    [ObservableProperty] private Visibility _resultVisibility = Visibility.Collapsed;

    [ObservableProperty] private string _signalSource = string.Empty;





    public CreateCaseViewModel([CanBeNull] ICaseFlowEngine caseFlowEngine, [CanBeNull] ILogger<CreateCaseViewModel> logger)
    {
        _caseFlowEngine = caseFlowEngine;
        _logger = logger;
    }





    public void OnNavigatedFrom() { }





    public void OnNavigatedTo([CanBeNull] object parameter)
    {
        // Reset form on navigation
        Description = string.Empty;
        SignalSource = string.Empty;
        ResultMessage = string.Empty;
        ResultVisibility = Visibility.Collapsed;
    }





    private bool CanCreateCase() => !IsBusy && !string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(SignalSource);





    [RelayCommand(CanExecute = nameof(CanCreateCase))]
    private async Task CreateCaseAsync()
    {
        IsBusy = true;
        ResultVisibility = Visibility.Collapsed;

        try
        {
            Signal signal = new(Description, SignalSource);

            Guid caseId = await _caseFlowEngine.CreateCaseAsync(signal);

            ResultMessage = $"Case created successfully. Case ID: {caseId}";
            ResultVisibility = Visibility.Visible;

            _logger.LogInformation("Created case {CaseId} via admin UI", caseId);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Error creating case: {ex.Message}";
            ResultVisibility = Visibility.Visible;
            _logger.LogError(ex, "Failed to create case via admin UI");
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
        ResultVisibility = Visibility.Collapsed;
    }
}