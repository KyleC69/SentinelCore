// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         SettingsPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;

using JetBrains.Annotations;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class SettingsPage : Page
{
    public SettingsPage([CanBeNull] SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}