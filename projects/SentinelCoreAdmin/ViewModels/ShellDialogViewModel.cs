// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ShellDialogViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;




namespace SentinelCoreAdmin.ViewModels;





public class ShellDialogViewModel : ObservableObject
{
    private ICommand _closeCommand;

    public ICommand CloseCommand
    {
        get => _closeCommand ?? (_closeCommand = new RelayCommand(OnClose));
    }

    public Action<bool?> SetResult { get; set; }








    private void OnClose()
    {
        bool result = true;
        SetResult(result);
    }
}