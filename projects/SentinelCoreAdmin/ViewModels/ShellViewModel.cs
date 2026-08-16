// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ShellViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using SentinelCoreAdmin.Contracts.Services;




namespace SentinelCoreAdmin.ViewModels;





// You can show pages in different ways (update main view, navigate, right pane, new windows or dialog)
// using the NavigationService, RightPaneService and WindowManagerService.
// Read more about MenuBar project type here:
// https://github.com/microsoft/TemplateStudio/blob/main/docs/WPF/projectTypes/menubar.md
public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IRightPaneService _rightPaneService;








    public ShellViewModel([CanBeNull] INavigationService navigationService, [CanBeNull] IRightPaneService rightPaneService)
    {
        _navigationService = navigationService;
        _rightPaneService = rightPaneService;
    }








    private bool CanGoBack() => _navigationService.CanGoBack;








    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => _navigationService.GoBack();








    [RelayCommand]
    private void Loaded() => _navigationService.Navigated += OnNavigated;








    [RelayCommand]
    private void MenuFileExit() => Application.Current.Shutdown();








    [RelayCommand]
    private void MenuFileSettings() => _rightPaneService.OpenInRightPane(typeof(SettingsViewModel).FullName);








    [RelayCommand]
    private void MenuViewsCoreChat() => _navigationService.NavigateTo(typeof(CoreChatViewModel).FullName, null, true);








    [RelayCommand]
    private void MenuViewsTraceLog() => _navigationService.NavigateTo(typeof(TraceLogViewModel).FullName, null, true);








    private void OnNavigated([CanBeNull] object sender, [CanBeNull] string viewModelName) => GoBackCommand.NotifyCanExecuteChanged();








    [RelayCommand]
    private void Unloaded()
    {
        _rightPaneService.CleanUp();
        _navigationService.Navigated -= OnNavigated;
    }
}