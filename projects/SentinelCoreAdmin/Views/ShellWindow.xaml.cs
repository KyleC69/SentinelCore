// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ShellWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;

using JetBrains.Annotations;

using MahApps.Metro.Controls;

using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow([CanBeNull] ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }








    public void CloseWindow() =>
            this.Close();








    [CanBeNull]
    public Frame GetNavigationFrame() => shellFrame;








    [CanBeNull]
    public Frame GetRightPaneFrame() => rightPaneFrame;








    [CanBeNull]
    public SplitView GetSplitView() => splitView;








    public void ShowWindow() =>
            this.Show();
}