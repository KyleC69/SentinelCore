// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         SettingsViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;
using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Models;




namespace SentinelCoreAdmin.ViewModels;





// TODO: Change the URL for your privacy policy in the appsettings.json file, currently set to https://YourPrivacyUrlGoesHere
public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private readonly AppConfig _appConfig;
    private readonly IApplicationInfoService _applicationInfoService;
    private readonly IIdentityService _identityService;
    private readonly ISystemService _systemService;

    [ObservableProperty] private AppTheme _theme;

    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty] private UserViewModel? _user;

    private readonly IUserDataService _userDataService;

    [ObservableProperty] private string? _versionDescription;








    public SettingsViewModel([NotNull] IOptions<AppConfig> appConfig, [CanBeNull] IThemeSelectorService themeSelectorService, [CanBeNull] ISystemService systemService, [CanBeNull] IApplicationInfoService applicationInfoService, [CanBeNull] IUserDataService userDataService, [CanBeNull] IIdentityService identityService)
    {
        _appConfig = appConfig.Value;
        _themeSelectorService = themeSelectorService;
        _systemService = systemService;
        _applicationInfoService = applicationInfoService;
        _userDataService = userDataService;
        _identityService = identityService;
    }








    public void OnNavigatedFrom() => UnregisterEvents();








    public void OnNavigatedTo([CanBeNull] object parameter)
    {
        VersionDescription = $"{Properties.Resources.AppDisplayName} - {_applicationInfoService.GetVersion()}";
        Theme = _themeSelectorService.GetCurrentTheme();
        _identityService.LoggedOut += OnLoggedOut;
        _userDataService.UserDataUpdated += OnUserDataUpdated;
        User = _userDataService.GetUser();
    }








    [RelayCommand]
    private async Task LogOutAsync() => await _identityService.LogoutAsync();








    private void OnLoggedOut(object? sender, EventArgs e) => UnregisterEvents();


    private void OnUserDataUpdated(object? sender, UserViewModel userData) => User = userData;








    [RelayCommand]
    private void PrivacyStatement() => _systemService.OpenInWebBrowser(_appConfig.PrivacyStatement);








    [RelayCommand]
    private void SetTheme([NotNull] string themeName)
    {
        AppTheme theme = (AppTheme)Enum.Parse(typeof(AppTheme), themeName);
        _themeSelectorService.SetTheme(theme);
    }








    private void UnregisterEvents()
    {
        _identityService.LoggedOut -= OnLoggedOut;
        _userDataService.UserDataUpdated -= OnUserDataUpdated;
    }
}