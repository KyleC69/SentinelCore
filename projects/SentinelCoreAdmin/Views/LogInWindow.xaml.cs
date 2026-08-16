// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         LogInWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using JetBrains.Annotations;

using MahApps.Metro.Controls;

using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class LogInWindow : MetroWindow, ILogInWindow
{
    public LogInWindow([CanBeNull] LogInViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }








    public void CloseWindow() =>
            this.Close();








    public void ShowWindow() =>
            this.Show();
}