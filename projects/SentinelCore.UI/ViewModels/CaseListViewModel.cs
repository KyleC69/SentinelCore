// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseListViewModel.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using SentinelCore.Cfe;
using SentinelCore.Contracts;
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

    [ObservableProperty] private ObservableCollection<CaseRow> _cases = new();

    [ObservableProperty] private ObservableCollection<CaseDetailItem> _detailCases = new();

    [ObservableProperty] private string _drillDownHeader = string.Empty;

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
        LoadCasesCommand.Execute(null);
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
                items.Add(new CaseDetailItem { CaseId = c.CaseId, Status = c.Status, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt });
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
