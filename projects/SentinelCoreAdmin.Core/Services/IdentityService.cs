// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         IdentityService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Net.NetworkInformation;

using Microsoft.Identity.Client;

using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Core.Helpers;




namespace SentinelCoreAdmin.Core.Services;





/// <summary>
///     Modern MSAL-based identity service for Windows desktop (WPF) applications.
///     <para>
///         Supports the Web Account Manager (WAM) broker for Windows 10+ which provides:
///         <list type="bullet">
///             <item>OS-integrated authentication (including UAC elevation scenarios)</item>
///             <item>Single sign-on with Windows accounts</item>
///             <item>Passkey / FIDO2 support</item>
///             <item>Secure token cache via DPAPI</item>
///         </list>
///     </para>
///     <para>
///         To enable WAM, call <see cref="SetBuilderAction" /> from the host application
///         (which targets Windows) and apply
///         <c>builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))</c>.
///         On older Windows versions the broker gracefully falls back to a browser-based flow.
///     </para>
/// </summary>
public class IdentityService : IIdentityService
{
    private AuthenticationResult _authenticationResult;

    /// <summary>
    ///     Allows the host application to modify the <see cref="PublicClientApplicationBuilder" />
    ///     before <see cref="IPublicClientApplication" /> is built. The host should use this
    ///     to enable platform-specific features such as the WAM broker on Windows:
    ///     <c>builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))</c>.
    /// </summary>
    private Action<PublicClientApplicationBuilder> _builderAction;

    private IPublicClientApplication _client;
    private readonly string[] _graphScopes = new[] { "User.Read" };
    private readonly IIdentityCacheService _identityCacheService;

    /// <summary>
    ///     WPF window handle callback — set by the host application so that WAM
    ///     broker dialogs (UAC prompts, account picker) are parented correctly.
    /// </summary>
    private Func<IntPtr> _parentWindowHandle;








    public IdentityService(IIdentityCacheService identityCacheService)
    {
        _identityCacheService = identityCacheService;
    }








    /// <inheritdoc />
    public Task<bool> AcquireTokenSilentAsync(CancellationToken cancellationToken = default) => AcquireTokenSilentAsync(_graphScopes, cancellationToken);








    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
    {
        bool acquireTokenSuccess = await AcquireTokenSilentAsync(scopes, cancellationToken).ConfigureAwait(false);
        if (acquireTokenSuccess)
        {
            return _authenticationResult.AccessToken;
        }

        try
        {
            IEnumerable<IAccount> accounts = await _client.GetAccountsAsync().ConfigureAwait(false);
            AcquireTokenInteractiveParameterBuilder builder = _client.AcquireTokenInteractive(scopes).WithAccount(accounts.FirstOrDefault());

            if (_parentWindowHandle != null)
            {
                builder.WithParentActivityOrWindow(_parentWindowHandle);
            }

            _authenticationResult = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return _authenticationResult.AccessToken;
        }
        catch (MsalException)
        {
            _authenticationResult = null;
            LoggedOut?.Invoke(this, EventArgs.Empty);
            return string.Empty;
        }
    }








    /// <inheritdoc />
    public Task<string> GetAccessTokenForGraphAsync(CancellationToken cancellationToken = default) => GetAccessTokenAsync(_graphScopes, cancellationToken);








    public string GetAccountUserName()
    {
        return _authenticationResult?.Account?.Username;
    }








    /// <inheritdoc />
    public async Task<LoginResultType> InitializeAndLoginAsync(AccountType accountType, string clientId, string redirectUri = null, string tenant = null, CancellationToken cancellationToken = default)
    {
        InitializeWithAccountType(accountType, clientId, redirectUri, tenant);
        return await LoginAsync(cancellationToken).ConfigureAwait(false);
    }








    /// <inheritdoc />
    public void InitializeWithAccountType(AccountType accountType, string clientId, string redirectUri = null, string tenant = null)
    {
        PublicClientApplicationBuilder builder = PublicClientApplicationBuilder.Create(clientId).WithDefaultRedirectUri();

        builder = accountType switch
        {
                AccountType.AadAndPersonalMsAccounts => builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount),
                AccountType.PersonalMsAccounts => builder.WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount),
                AccountType.AadMultipleOrgs => builder.WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs),
                AccountType.AadSingleOrg => builder.WithAuthority(AzureCloudInstance.AzurePublic, tenant ?? "common"),
                _ => builder.WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
        };

        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            builder.WithRedirectUri(redirectUri);
        }

        // Allow the host to inject platform-specific configuration (e.g. WAM broker).
        _builderAction?.Invoke(builder);

        _client = builder.Build();
        ConfigureCache();
    }








    public bool IsAuthorized()
    {
        // TODO: Add role / group membership checks here if needed.
        return true;
    }








    public bool IsLoggedIn() => _authenticationResult != null;


    public event EventHandler LoggedIn;
    public event EventHandler LoggedOut;








    /// <inheritdoc />
    public async Task<LoginResultType> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return LoginResultType.NoNetworkAvailable;
        }

        try
        {
            IEnumerable<IAccount> accounts = await _client.GetAccountsAsync().ConfigureAwait(false);
            AcquireTokenInteractiveParameterBuilder builder = _client.AcquireTokenInteractive(_graphScopes).WithAccount(accounts.FirstOrDefault());

            // Parent the WAM dialog to the WPF window so UAC and account-picker
            // prompts appear in the correct context.
            if (_parentWindowHandle != null)
            {
                builder.WithParentActivityOrWindow(_parentWindowHandle);
            }

            _authenticationResult = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            if (!IsAuthorized())
            {
                _authenticationResult = null;
                return LoginResultType.Unauthorized;
            }

            LoggedIn?.Invoke(this, EventArgs.Empty);
            return LoginResultType.Success;
        }
        catch (OperationCanceledException)
        {
            return LoginResultType.CancelledByUser;
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            return LoginResultType.CancelledByUser;
        }
        catch (MsalException ex)
        {
            System.Diagnostics.Debug.WriteLine($"MSAL login error: {ex.ErrorCode} — {ex.Message}");
            return LoginResultType.UnknownError;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            return LoginResultType.UnknownError;
        }
    }








    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await _client.GetAccountsAsync().ConfigureAwait(false);
            foreach (IAccount account in accounts) await _client.RemoveAsync(account).ConfigureAwait(false);

            _authenticationResult = null;
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }
        catch (MsalException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.ErrorCode} — {ex.Message}");
        }
    }








    private async Task<bool> AcquireTokenSilentAsync(string[] scopes, CancellationToken cancellationToken)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        IEnumerable<IAccount> accounts = await _client.GetAccountsAsync().ConfigureAwait(false);
        IAccount account = accounts.FirstOrDefault();

        // No cached account means there is no session to renew silently.
        // Return false so the caller falls back to AcquireTokenInteractive
        // instead of throwing MsalUiRequiredException (user_null).
        if (account is null)
        {
            return false;
        }

        try
        {
            AcquireTokenSilentParameterBuilder builder = _client.AcquireTokenSilent(scopes, account);
            _authenticationResult = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MsalUiRequiredException)
        {
            // Interactive authentication is required — caller should fall back to AcquireTokenInteractive.
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (MsalException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Silent auth failed: {ex.ErrorCode} — {ex.Message}");
            return false;
        }
    }








    private void ConfigureCache()
    {
        if (_identityCacheService != null)
        {
            _client.UserTokenCache.SetBeforeAccess(args =>
            {
                byte[] data = _identityCacheService.ReadMsalToken();
                if (data != null && data.Length > 0)
                {
                    args.TokenCache.DeserializeMsalV3(data);
                }
            });
            _client.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    _identityCacheService.SaveMsalToken(args.TokenCache.SerializeMsalV3());
                }
            });
        }
    }








    /// <summary>
    ///     Allows the host application to inject platform-specific builder configuration
    ///     (e.g. WAM broker) before the <see cref="IPublicClientApplication" /> is built.
    /// </summary>
    /// <example>
    ///     <code>
    /// identityService.SetBuilderAction(builder =>
    ///     builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)));
    ///     </code>
    /// </example>
    public void SetBuilderAction(Action<PublicClientApplicationBuilder> configure)
    {
        _builderAction = configure;
    }








    /// <summary>
    ///     Sets the parent window handle callback used by WAM broker dialogs.
    ///     Call this from the WPF host with <c>() => new WindowInteropHelper(this).Handle</c>
    ///     so UAC and account-picker prompts are parented to the correct window.
    /// </summary>
    public void SetParentWindowHandle(Func<IntPtr> windowHandleProvider)
    {
        _parentWindowHandle = windowHandleProvider;
    }
}