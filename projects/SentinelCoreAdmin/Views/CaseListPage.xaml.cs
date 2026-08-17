// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CaseListPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;
using System.Windows.Controls;

using JetBrains.Annotations;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class CaseListPage : Page
{
    private readonly CaseListViewModel _viewModel;





    public CaseListPage([CanBeNull] CaseListViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }





    private void SummaryDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.DrillIntoStatusCommand.CanExecute(null) == true)
        {
            _viewModel.DrillIntoStatusCommand.Execute(null);
        }
    }





    private void DetailDataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.OpenCaseDetailCommand.CanExecute(null) == true)
        {
            _viewModel.OpenCaseDetailCommand.Execute(null);
        }
    }
}