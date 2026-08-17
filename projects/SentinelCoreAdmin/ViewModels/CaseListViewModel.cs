// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CaseListViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Collections.ObjectModel;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using SentinelCore.Cfe;
using SentinelCore.Contracts;
using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;




namespace SentinelCoreAdmin.ViewModels;


/// <summary>
///     View-model for the Case List page.
///     Displays a summary of case counts by status. Double-clicking a
///     status row drills down to show individual cases at that status level.
/// </summary>
public partial class CaseListViewModel : ObservableObject, INavigationAware
{
    private readonly ICaseFlowEngine _caseFlowEngine;
    private readonly ILogger<CaseListViewModel> _logger;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<CaseRow> _cases = new();

    [ObservableProperty] private bool _isDrilledDown;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private CaseStatus? _selectedStatusFilter;

    [ObservableProperty] private CaseRow? _selectedCase;

    [ObservableProperty] private ObservableCollection<CaseDetailItem> _detailCases = new();

    [ObservableProperty] private CaseDetailItem? _selectedDetailCase;

    [ObservableProperty] private string _drillDownHeader = string.Empty;





    public CaseListViewModel(
        [CanBeNull] ICaseFlowEngine caseFlowEngine,
        [CanBeNull] ILogger<CaseListViewModel> logger,
        [CanBeNull] INavigationService navigationService)
    {
        _caseFlowEngine = caseFlowEngine;
        _logger = logger;
        _navigationService = navigationService;
    }





    /// <summary>
    ///     Available case statuses for the filter combo box.
    /// </summary>
    public IReadOnlyList<CaseStatus> AvailableStatuses { get; } =
        Enum.GetValues<CaseStatus>().Where(s => s != CaseStatus.Initialized).ToList();





    public void OnNavigatedFrom() { }





    public async void OnNavigatedTo([CanBeNull] object parameter)
    {
        await LoadCasesAsync();
    }





    [RelayCommand]
    private async Task LoadCasesAsync()
    {
        IsLoading = true;

        try
        {
            ObservableCollection<CaseRow> rows = new();

            foreach (CaseStatus status in Enum.GetValues<CaseStatus>())
            {
                try
                {
                    int count = await _caseFlowEngine.GetCaseCountByStatusAsync(status);
                    rows.Add(new CaseRow { Status = status, Count = count });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load case count for status {Status}", status);
                    rows.Add(new CaseRow { Status = status, Count = 0 });
                }
            }

            Cases = rows;
            _logger.LogTrace("Case list loaded — {Count} status entries", rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cases");
        }
        finally
        {
            IsLoading = false;
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

        try
        {
            IReadOnlyList<Case> cases = await _caseFlowEngine.GetCasesByStatusAsync(status.Value);
            ObservableCollection<CaseDetailItem> items = new();

            foreach (Case c in cases)
            {
                items.Add(new CaseDetailItem
                {
                    CaseId = c.CaseId,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                });
            }

            DetailCases = items;
            IsDrilledDown = true;
            DrillDownHeader = $"Cases in status: {status.Value} ({items.Count})";
            _logger.LogTrace("Drilled down into {Status} — {Count} cases", status, items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cases for status {Status}", status);
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





    /// <summary>
    ///     Navigates to the Case Detail page with the selected case ID.
    /// </summary>
    [RelayCommand]
    private void OpenCaseDetail()
    {
        if (SelectedDetailCase is not null)
        {
            _navigationService.NavigateTo(typeof(CaseDetailViewModel).FullName, SelectedDetailCase.CaseId.ToString());
        }
    }





    [RelayCommand]
    private async Task GoBackToSummaryAsync()
    {
        IsDrilledDown = false;
        DetailCases.Clear();
        DrillDownHeader = string.Empty;
        await LoadCasesAsync();
    }
}





/// <summary>
///     Represents a row in the case summary grid (one per status).
/// </summary>
public sealed class CaseRow
{
    public CaseStatus Status { get; set; }
    public int Count { get; set; }
}





/// <summary>
///     Represents an individual case in the drill-down detail grid.
/// </summary>
public sealed class CaseDetailItem
{
    public Guid CaseId { get; set; }
    public CaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}