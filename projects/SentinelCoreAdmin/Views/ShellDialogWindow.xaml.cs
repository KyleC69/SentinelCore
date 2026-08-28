// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ShellDialogWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;

using JetBrains.Annotations;

using MahApps.Metro.Controls;

using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class ShellDialogWindow : MetroWindow, IShellDialogWindow
{
    public ShellDialogWindow([NotNull] ShellDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.SetResult = OnSetResult;
        DataContext = viewModel;
    }








    [CanBeNull]
    public Frame GetDialogFrame() => dialogFrame;








    private void OnSetResult(bool? result)
    {
        DialogResult = result;
        this.Close();
    }
}