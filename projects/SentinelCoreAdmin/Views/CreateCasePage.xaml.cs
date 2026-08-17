// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CreateCasePage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;

using JetBrains.Annotations;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class CreateCasePage : Page
{





    public CreateCasePage([CanBeNull] CreateCaseViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}