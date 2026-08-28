// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         TraceLogPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;

using JetBrains.Annotations;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class TraceLogPage : Page
{

    public TraceLogPage([CanBeNull] TraceLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}