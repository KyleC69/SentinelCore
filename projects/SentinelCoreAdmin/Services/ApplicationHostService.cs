// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ApplicationHostService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

using SentinelCoreAdmin.Contracts.Activation;
using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Core.Helpers;
using SentinelCoreAdmin.Core.Services;
using SentinelCoreAdmin.Models;
using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Services;





public class ApplicationHostService : IHostedService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly AppConfig _appConfig;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IIdentityService _identityService;
    private bool _isInitialized;
    private ILogInWindow? _logInWindow;
    private readonly ILogger<ApplicationHostService> _logger;
    private readonly INavigationService _navigationService;
    private readonly IPersistAndRestoreService _persistAndRestoreService;

    private readonly IRightPaneService _rightPaneService;
    private readonly IServiceProvider _serviceProvider;
    private IShellWindow? _shellWindow;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IToastNotificationsService _toastNotificationsService;
    private readonly IUserDataService _userDataService;








    public ApplicationHostService(IServiceProvider serviceProvider, IEnumerable<IActivationHandler> activationHandlers, INavigationService navigationService, IRightPaneService rightPaneService, IThemeSelectorService themeSelectorService, IPersistAndRestoreService persistAndRestoreService, IToastNotificationsService toastNotificationsService, IIdentityService identityService, IUserDataService userDataService, IOptions<AppConfig> config, IHostApplicationLifetime hostLifetime, ILogger<ApplicationHostService> logger)
    {
        _serviceProvider = serviceProvider;
        _activationHandlers = activationHandlers;
        _navigationService = navigationService;
        _rightPaneService = rightPaneService;
        _themeSelectorService = themeSelectorService;
        _persistAndRestoreService = persistAndRestoreService;
        _toastNotificationsService = toastNotificationsService;
        _identityService = identityService;
        _userDataService = userDataService;
        _hostLifetime = hostLifetime;
        _logger = logger;
        _appConfig = config.Value;
    }








    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Register a callback so that when the host signals stopping (e.g. via IHostApplicationLifetime),
        // we begin cooperative shutdown of in-flight work.
        _hostLifetime.ApplicationStopping.Register(() => { _logger.LogInformation("Application stopping — cooperative shutdown signalled."); });

        // Initialize services that you need before app activation
        await InitializeAsync();

        if (!_isInitialized)
        {
            // Initialize identity with the configured (or previously selected) account type
            AccountType accountType = GetSavedAccountType();
            _identityService.InitializeWithAccountType(accountType, _appConfig.IdentityClientId, "http://localhost");

            // Wire WAM broker to the main WPF window so UAC and account-picker dialogs
            // are parented correctly.
            if (_identityService is IdentityService concrete)
            {
                // Enable the Windows Account Manager (WAM) broker for OS-integrated auth,
                // UAC elevation prompts, SSO with Windows accounts, and passkey support.
                concrete.SetBuilderAction(builder => builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)));

                concrete.SetParentWindowHandle(() =>
                {
                    IShellWindow shell = App.Current.Windows.OfType<IShellWindow>().FirstOrDefault();
                    return shell is System.Windows.Window w ? new System.Windows.Interop.WindowInteropHelper(w).Handle : IntPtr.Zero;
                });
            }

            bool silentLoginSuccess = await _identityService.AcquireTokenSilentAsync(cancellationToken);
            if (!silentLoginSuccess || !_identityService.IsAuthorized())
            {
                _logInWindow = _serviceProvider.GetService(typeof(ILogInWindow)) as ILogInWindow;
                if (_logInWindow is null)
                {
                    _logger.LogError("Failed to resolve ILogInWindow from service provider.");
                    return;
                }

                _logInWindow.ShowWindow();
                await StartupAsync();
                _isInitialized = true;
                return;
            }
        }

        await HandleActivationAsync();

        // Tasks after activation
        await StartupAsync();
        _isInitialized = true;
    }








    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApplicationHostService stopping — persisting data and unsubscribing events.");

        _persistAndRestoreService.PersistData();

        _identityService.LoggedIn -= OnLoggedIn;
        _identityService.LoggedOut -= OnLoggedOut;

        await Task.CompletedTask;
    }








    private AccountType GetSavedAccountType()
    {
        if (App.Current.Properties.Contains("IdentityAccountType") && App.Current.Properties["IdentityAccountType"] is string saved && Enum.TryParse(saved, out AccountType result))
        {
            return result;
        }

        // Fall back to config default
        if (Enum.TryParse(_appConfig.IdentityAccountType, out AccountType configDefault))
        {
            return configDefault;
        }

        return AccountType.AadAndPersonalMsAccounts;
    }








    private async Task HandleActivationAsync()
    {
        IActivationHandler? activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle());

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync();
        }

        await Task.CompletedTask;

        if (App.Current.Windows.OfType<IShellWindow>().Count() == 0)
        {
            // Default activation that navigates to the apps default page
            _shellWindow = _serviceProvider.GetService(typeof(IShellWindow)) as IShellWindow;
            if (_shellWindow is null)
            {
                _logger.LogError("Failed to resolve IShellWindow from service provider.");
                return;
            }

            _navigationService.Initialize(_shellWindow.GetNavigationFrame());
            _rightPaneService.Initialize(_shellWindow.GetRightPaneFrame(), _shellWindow.GetSplitView());
            _shellWindow.ShowWindow();
            _navigationService.NavigateTo(typeof(CoreChatViewModel).FullName!);
            await Task.CompletedTask;
        }
    }








    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _persistAndRestoreService.RestoreData();
            _themeSelectorService.InitializeTheme();
            _userDataService.Initialize();
            _identityService.LoggedIn += OnLoggedIn;
            _identityService.LoggedOut += OnLoggedOut;
            await Task.CompletedTask;
        }
    }








    private async void OnLoggedIn(object? sender, EventArgs e)
    {
        await HandleActivationAsync();
        _logInWindow?.CloseWindow();
    }








    private void OnLoggedOut(object? sender, EventArgs e)
    {
        _logInWindow = _serviceProvider.GetService(typeof(ILogInWindow)) as ILogInWindow;
        if (_logInWindow is null)
        {
            _logger.LogError("Failed to resolve ILogInWindow from service provider.");
            return;
        }

        _logInWindow.ShowWindow();

        _shellWindow?.CloseWindow();
        _navigationService.UnsubscribeNavigation();
    }








    private async Task StartupAsync()
    {
        if (!_isInitialized)
        {
            _toastNotificationsService.ShowToastNotificationSample();
            await Task.CompletedTask;
        }
    }
}