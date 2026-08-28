// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         LogInViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Core.Helpers;
using SentinelCoreAdmin.Models;
using SentinelCoreAdmin.Properties;




namespace SentinelCoreAdmin.ViewModels;





public partial class LogInViewModel : ObservableObject
{
    private readonly AppConfig _appConfig;
    private readonly IIdentityService _identityService;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    [ObservableProperty] private AccountType _selectedAccountType = AccountType.AadAndPersonalMsAccounts;

    [ObservableProperty] private string? _statusMessage;








    public LogInViewModel([CanBeNull] IIdentityService identityService, [NotNull] IOptions<AppConfig> config)
    {
        _identityService = identityService;
        _appConfig = config.Value;
    }








    public IEnumerable<AccountType> AccountTypes { get; } = Enum.GetValues<AccountType>();


    private bool CanLogin() => !IsBusy;








    [CanBeNull]
    private string GetStatusMessage(LoginResultType loginResult)
    {
        return loginResult switch
        {
                LoginResultType.Unauthorized => Resources.StatusUnauthorized,
                LoginResultType.NoNetworkAvailable => Resources.StatusNoNetworkAvailable,
                LoginResultType.UnknownError => Resources.StatusLoginFails,
                LoginResultType.Success or LoginResultType.CancelledByUser => string.Empty,
                _ => string.Empty
        };
    }








    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;

        LoginResultType loginResult = await _identityService.InitializeAndLoginAsync(SelectedAccountType, _appConfig.IdentityClientId, "http://localhost", _appConfig.IdentityTenant);

        if (loginResult == LoginResultType.Success)
        {
            // Persist the chosen account type so it can be used for silent re-auth on next startup
            App.Current.Properties["IdentityAccountType"] = SelectedAccountType.ToString();
        }

        StatusMessage = GetStatusMessage(loginResult);
        IsBusy = false;
    }
}