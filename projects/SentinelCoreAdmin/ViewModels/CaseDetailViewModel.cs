// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CaseDetailViewModel.cs
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
///     View-model for the Case Detail page.
///     Allows users to view a case by ID and advance its status.
/// </summary>
public partial class CaseDetailViewModel : ObservableObject, INavigationAware
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ILogger<CaseDetailViewModel> _logger;

    [ObservableProperty] private string _caseIdText = string.Empty;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _resultMessage = string.Empty;

    [ObservableProperty] private Visibility _resultVisibility = Visibility.Collapsed;

    [ObservableProperty] private CaseStatus? _selectedTargetStatus;

    [ObservableProperty] private string _statusInfo = string.Empty;





    public CaseDetailViewModel([CanBeNull] ICaseFlowEngine caseFlowEngine, [CanBeNull] ILogger<CaseDetailViewModel> logger)
    {
        _caseFlowEngine = caseFlowEngine;
        _logger = logger;
    }





    /// <summary>
    ///     Available case statuses for the advance target combo box.
    /// </summary>
    public IReadOnlyList<CaseStatus> AvailableStatuses { get; } = Enum.GetValues<CaseStatus>().ToList();





    public void OnNavigatedFrom() { }





    public void OnNavigatedTo([CanBeNull] object parameter)
    {
        CaseIdText = string.Empty;
        SelectedTargetStatus = null;
        StatusInfo = string.Empty;
        ResultMessage = string.Empty;
        ResultVisibility = Visibility.Collapsed;
    }





    private bool CanAdvanceCase() => !IsBusy && !string.IsNullOrWhiteSpace(CaseIdText) && SelectedTargetStatus is not null;





    [RelayCommand(CanExecute = nameof(CanAdvanceCase))]
    private async Task AdvanceCaseAsync()
    {
        IsBusy = true;
        ResultVisibility = Visibility.Collapsed;

        try
        {
            if (!Guid.TryParse(CaseIdText, out Guid caseId))
            {
                ResultMessage = "Invalid Case ID format. Please enter a valid GUID.";
                ResultVisibility = Visibility.Visible;
                return;
            }

            await _caseFlowEngine.AdvanceCaseAsync(caseId, SelectedTargetStatus!.Value);

            ResultMessage = $"Case {caseId} advanced to {SelectedTargetStatus.Value} successfully.";
            ResultVisibility = Visibility.Visible;

            _logger.LogInformation("Advanced case {CaseId} to {Status} via admin UI", caseId, SelectedTargetStatus.Value);
        }
        catch (InvalidOperationException ex)
        {
            ResultMessage = $"Transition not allowed: {ex.Message}";
            ResultVisibility = Visibility.Visible;
            _logger.LogWarning(ex, "Invalid case transition attempted via admin UI");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Error: {ex.Message}";
            ResultVisibility = Visibility.Visible;
            _logger.LogError(ex, "Failed to advance case via admin UI");
        }
        finally
        {
            IsBusy = false;
            AdvanceCaseCommand.NotifyCanExecuteChanged();
        }
    }





    [RelayCommand]
    private async Task LookupCaseAsync()
    {
        if (string.IsNullOrWhiteSpace(CaseIdText) || !Guid.TryParse(CaseIdText, out Guid caseId))
        {
            StatusInfo = "Enter a valid Case ID to look up.";
            return;
        }

        IsBusy = true;

        try
        {
            // Query case counts by status to give context
            List<string> lines = new() { $"Case ID: {caseId}" };

            foreach (CaseStatus status in Enum.GetValues<CaseStatus>())
            {
                try
                {
                    int count = await _caseFlowEngine.GetCaseCountByStatusAsync(status);
                    lines.Add($"  {status}: {count}");
                }
                catch
                {
                    // Skip statuses that fail
                }
            }

            StatusInfo = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            StatusInfo = $"Lookup failed: {ex.Message}";
            _logger.LogError(ex, "Case lookup failed");
        }
        finally
        {
            IsBusy = false;
        }
    }





    [RelayCommand]
    private void ClearForm()
    {
        CaseIdText = string.Empty;
        SelectedTargetStatus = null;
        StatusInfo = string.Empty;
        ResultMessage = string.Empty;
        ResultVisibility = Visibility.Collapsed;
    }
}