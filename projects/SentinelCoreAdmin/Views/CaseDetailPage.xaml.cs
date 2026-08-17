// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CaseDetailPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;

using JetBrains.Annotations;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class CaseDetailPage : Page
{





    public CaseDetailPage([CanBeNull] CaseDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}