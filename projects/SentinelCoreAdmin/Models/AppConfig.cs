// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         AppConfig.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Models;





public class AppConfig
{

    public string? AppPropertiesFileName { get; set; }

    public string? ConfigurationsFolder { get; set; }

    /// <summary>
    ///     The default account type used for silent re-authentication on startup.
    ///     Persisted to app properties at runtime when the user selects a different type.
    /// </summary>
    public string IdentityAccountType { get; set; } = "AadAndPersonalMsAccounts";

    public string? IdentityCacheDirectoryName { get; set; }

    public string? IdentityCacheFileName { get; set; }

    public string IdentityClientId { get; set; } = "aa4a7895-823f-470c-9007-4ad7d9babbf9";

    /// <summary>
    ///     The Azure AD tenant ID or domain name. Required when using AadSingleOrg account type.
    /// </summary>
    public string? IdentityTenant { get; set; }

    public string? PrivacyStatement { get; set; }
    public string? UserFileName { get; set; }
}