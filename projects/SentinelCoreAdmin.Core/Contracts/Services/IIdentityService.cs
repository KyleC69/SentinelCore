// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         IIdentityService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCoreAdmin.Core.Helpers;




namespace SentinelCoreAdmin.Core.Contracts.Services;





public interface IIdentityService
{

    Task<bool> AcquireTokenSilentAsync(CancellationToken cancellationToken = default);


    Task<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default);


    Task<string> GetAccessTokenForGraphAsync(CancellationToken cancellationToken = default);


    string? GetAccountUserName();








    /// <summary>
    ///     Initializes the MSAL client for the specified <paramref name="accountType" />
    ///     and performs an interactive login.
    ///     This is the single entry point for all initialization — the per-type
    ///     overloads have been consolidated here.
    /// </summary>
    /// <param name="accountType">The Azure AD account type to use.</param>
    /// <param name="clientId">The application (client) ID from Azure AD app registration.</param>
    /// <param name="redirectUri">Optional redirect URI for auth flows.</param>
    /// <param name="tenant">Optional tenant for single-org scenarios.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    Task<LoginResultType> InitializeAndLoginAsync(AccountType accountType, string clientId, string? redirectUri = null, string? tenant = null, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Initializes the MSAL client for the specified <paramref name="accountType" />
    ///     without performing a login. Used for silent re-authentication on startup.
    /// </summary>
    void InitializeWithAccountType(AccountType accountType, string clientId, string? redirectUri = null, string? tenant = null);








    bool IsAuthorized();


    bool IsLoggedIn();


    event EventHandler LoggedIn;

    event EventHandler LoggedOut;


    Task<LoginResultType> LoginAsync(CancellationToken cancellationToken = default);


    Task LogoutAsync(CancellationToken cancellationToken = default);
}