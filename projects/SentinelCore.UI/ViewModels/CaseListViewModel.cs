// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseListViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;




namespace SentinelCore.UI.ViewModels;





/// <summary>
///     View-model for the Case List page.
///     Displays a summary of case counts by status. Double-clicking a
///     status row drills down to show individual cases at that status level.
/// </summary>
public sealed partial class CaseListViewModel : ObservableObject, INavigationAware
{
    private readonly ICaseFlowEngine _caseFlowEngine;

    [ObservableProperty] private ObservableCollection<CaseRow> _cases = [];

    [ObservableProperty] private ObservableCollection<CaseDetailItem> _detailCases = [];

    [ObservableProperty] private string _drillDownHeader = string.Empty;

    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private bool _hasError;

    [ObservableProperty] private bool _isDrilledDown;

    [ObservableProperty] private bool _isLoading;

    private readonly ILogger<CaseListViewModel> _logger;

    private readonly INavigationService _navigationService;

    [ObservableProperty] private CaseRow? _selectedCase;

    [ObservableProperty] private CaseDetailItem? _selectedDetailCase;

    [ObservableProperty] private CaseStatus? _selectedStatusFilter;








    /// <summary>
    ///     Creates a new <see cref="CaseListViewModel" /> with required dependencies.
    /// </summary>
    /// <param name="caseFlowEngine">The case flow engine for querying case data.</param>
    /// <param name="logger">The logger for this view-model.</param>
    /// <param name="navigationService">The navigation service for page navigation.</param>
    public CaseListViewModel(ICaseFlowEngine caseFlowEngine, ILogger<CaseListViewModel> logger, INavigationService navigationService)
    {
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }








    /// <summary>
    ///     Available case statuses for the filter combo box.
    /// </summary>
    public IReadOnlyList<CaseStatus> AvailableStatuses { get; } = Enum.GetValues<CaseStatus>().Where(s => s != CaseStatus.Initialized).ToList();








    public void OnNavigatedFrom()
    {
    }








    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadCasesAsync();
    }








    [RelayCommand]
    private void ClearFilter()
    {
        SelectedStatusFilter = null;
        IsDrilledDown = false;
        DetailCases.Clear();
        DrillDownHeader = string.Empty;
    }








    [RelayCommand]
    private async Task DrillDownAsync(CaseStatus? status)
    {
        if (status is null)
        {
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            IReadOnlyList<Case> cases = await _caseFlowEngine.GetCasesByStatusAsync(status.Value);

            DetailCases.Clear();
            foreach (Case c in cases)
            {
                DetailCases.Add(new CaseDetailItem { CaseId = c.CaseId, Status = c.Status, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt });
            }

            IsDrilledDown = true;
            DrillDownHeader = $"Cases in status: {status.Value} ({DetailCases.Count})";
            _logger.LogTrace("Drilled down into {Status} — {Count} cases", status, DetailCases.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cases for status {Status}", status);
            HasError = true;
            ErrorMessage = $"Failed to load cases for status {status.Value}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }








    /// <summary>
    ///     Handles double-click on a summary row to drill down into that status level.
    /// </summary>
    [RelayCommand]
    private async Task DrillIntoStatusAsync()
    {
        if (SelectedCase is not null)
        {
            await DrillDownAsync(SelectedCase.Status);
        }
    }








    [RelayCommand]
    private async Task FilterByStatusAsync()
    {
        if (SelectedStatusFilter is null)
        {
            await LoadCasesAsync();
            return;
        }

        await DrillDownAsync(SelectedStatusFilter.Value);
    }








    [RelayCommand]
    private async Task GoBackToSummaryAsync()
    {
        IsDrilledDown = false;
        DetailCases.Clear();
        DrillDownHeader = string.Empty;
        await LoadCasesAsync();
    }








    [RelayCommand]
    private async Task LoadCasesAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            // Single grouped query instead of one round-trip per status.
            IReadOnlyDictionary<CaseStatus, int> counts = await _caseFlowEngine.GetCaseStatusCountsAsync();

            Cases.Clear();
            foreach (CaseStatus status in Enum.GetValues<CaseStatus>())
            {
                Cases.Add(new CaseRow { Status = status, Count = counts.TryGetValue(status, out int count) ? count : 0 });
            }

            _logger.LogTrace("Case list loaded — {Count} status entries", Cases.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cases");
            HasError = true;
            ErrorMessage = $"Failed to load case counts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }








    /// <summary>
    ///     Navigates to the Case Detail page with the selected case ID.
    /// </summary>
    [RelayCommand]
    private void OpenCaseDetail()
    {
        if (SelectedDetailCase is not null)
        {
            _navigationService.NavigateTo(typeof(CaseDetailViewModel).FullName!, SelectedDetailCase.CaseId.ToString());
        }
    }
}