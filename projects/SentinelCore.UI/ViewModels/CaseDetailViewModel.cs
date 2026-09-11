// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseDetailViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.UI.Services;




namespace SentinelCore.UI.ViewModels;





/// <summary>
///     View-model for the Case Detail page.
///     Allows users to view a case by ID and advance its status.
///     The case ID may arrive via navigation from the case list drill-down.
/// </summary>
public sealed partial class CaseDetailViewModel : ObservableObject, INavigationAware
{

    /// <summary>
    ///     Gets the statuses the looked-up case may legally advance to.
    /// </summary>
    [ObservableProperty] private IReadOnlyList<CaseStatus> _allowedTransitions = [];

    private readonly ICaseFlowEngine _caseFlowEngine;

    [ObservableProperty] private string _caseIdText = string.Empty;

    [ObservableProperty] private bool _isBusy;

    private readonly ILogger<CaseDetailViewModel> _logger;

    [ObservableProperty] private string _resultMessage = string.Empty;

    [ObservableProperty] private CaseStatus? _selectedTargetStatus;

    [ObservableProperty] private bool _showResult;

    [ObservableProperty] private string _statusInfo = string.Empty;








    /// <summary>
    ///     Creates a new <see cref="CaseDetailViewModel" /> with required dependencies.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine for case operations.</param>
    /// <param name="logger">The logger for this view-model.</param>
    public CaseDetailViewModel(ICaseFlowEngine caseFlowEngine, ILogger<CaseDetailViewModel> logger)
    {
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }








    /// <summary>
    ///     Available case statuses for the advance target combo box. Populated with
    ///     the legal transitions for the looked-up case's current status; falls back
    ///     to the full lifecycle until a case is loaded.
    /// </summary>
    public IReadOnlyList<CaseStatus> AvailableStatuses { get; } = Enum.GetValues<CaseStatus>().ToList();








    public void OnNavigatedFrom()
    {
    }








    public void OnNavigatedTo(object? parameter)
    {
        CaseIdText = parameter as string ?? string.Empty;
        SelectedTargetStatus = null;
        StatusInfo = string.Empty;
        ResultMessage = string.Empty;
        ShowResult = false;
        AllowedTransitions = [];

        if (!string.IsNullOrWhiteSpace(CaseIdText))
        {
            _ = LookupCaseAsync();
        }
    }








    [RelayCommand(CanExecute = nameof(CanAdvanceCase))]
    private async Task AdvanceCaseAsync()
    {
        IsBusy = true;
        ShowResult = false;

        try
        {
            if (!Guid.TryParse(CaseIdText, out Guid caseId))
            {
                ResultMessage = "Invalid Case ID format. Please enter a valid GUID.";
                ShowResult = true;
                return;
            }

            await _caseFlowEngine.AdvanceCaseAsync(caseId, SelectedTargetStatus!.Value);

            ResultMessage = $"Case {caseId} advanced to {SelectedTargetStatus.Value} successfully.";
            ShowResult = true;

            _logger.LogInformation("Advanced case {CaseId} to {Status} via UI", caseId, SelectedTargetStatus.Value);
        }
        catch (InvalidOperationException ex)
        {
            ResultMessage = $"Transition not allowed: {ex.Message}";
            ShowResult = true;
            _logger.LogWarning(ex, "Invalid case transition attempted via UI");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Error: {ex.Message}";
            ShowResult = true;
            _logger.LogError(ex, "Failed to advance case via UI");
        }
        finally
        {
            IsBusy = false;
            AdvanceCaseCommand.NotifyCanExecuteChanged();
        }
    }








    private bool CanAdvanceCase() => !IsBusy && !string.IsNullOrWhiteSpace(CaseIdText) && SelectedTargetStatus is not null;








    [RelayCommand]
    private void ClearForm()
    {
        CaseIdText = string.Empty;
        SelectedTargetStatus = null;
        StatusInfo = string.Empty;
        ResultMessage = string.Empty;
        ShowResult = false;
        AllowedTransitions = [];
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
            Case? found = await _caseFlowEngine.GetCaseByIdAsync(caseId);

            if (found is null)
            {
                StatusInfo = $"No case found with ID {caseId}.";
                AllowedTransitions = [];
                return;
            }

            StatusInfo = $"Case ID: {found.CaseId}{Environment.NewLine}Status: {found.Status}{Environment.NewLine}Created: {found.CreatedAt:g}{Environment.NewLine}Updated: {(found.UpdatedAt.HasValue ? found.UpdatedAt.Value.ToString("g") : "—")}";

            // Only offer the statuses the lifecycle actually allows from here.
            AllowedTransitions = _caseFlowEngine.GetAllowedTransitions(found.Status);
        }
        catch (Exception ex)
        {
            StatusInfo = $"Lookup failed: {ex.Message}";
            _logger.LogError(ex, "Case lookup failed");
            AllowedTransitions = [];
        }
        finally
        {
            IsBusy = false;
        }
    }
}