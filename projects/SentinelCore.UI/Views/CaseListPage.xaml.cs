// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseListPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Windows;
using System.Windows.Controls;

using SentinelCore.UI.ViewModels;



namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Case List page.
///     Responsibilities scoped to this file: ViewModel wiring and
///     double-click row handling for the summary and drill-down grids.
/// </summary>
public partial class CaseListPage : Page
{
    private readonly CaseListViewModel _viewModel;





    public CaseListPage(CaseListViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
    }





    private void DetailDataGrid_MouseDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.OpenCaseDetailCommand.CanExecute(null))
        {
            _viewModel.OpenCaseDetailCommand.Execute(null);
        }
    }





    private void SummaryDataGrid_MouseDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.DrillIntoStatusCommand.CanExecute(null))
        {
            _viewModel.DrillIntoStatusCommand.Execute(null);
        }
    }
}
