// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         AppConfig.cs
// Author: Kyle L. Crowler
// Build Num:  083003



namespace SentinelCore.UI.Models;


/// <summary>
///     Application configuration model bound from appsettings.json
///     or environment variables at runtime.
///     Identity credentials must be supplied via configuration —
///     no hard-coded defaults are provided.
/// </summary>
public class AppConfig
{
    /// <summary>
    ///     The account type used for silent re-authentication on startup.
    ///     Persisted to app properties at runtime when the user selects a different type.
    ///     Must be supplied via configuration (e.g. appsettings.json or environment variable).
    /// </summary>
    public string IdentityAccountType { get; set; } = string.Empty;

    /// <summary>
    ///     The directory name for the identity token cache.
    /// </summary>
    public string? IdentityCacheDirectoryName { get; set; }

    /// <summary>
    ///     The file name for the identity token cache.
    /// </summary>
    public string? IdentityCacheFileName { get; set; }

    /// <summary>
    ///     The Azure AD client ID. Must be supplied via configuration.
    /// </summary>
    public string IdentityClientId { get; set; } = string.Empty;

    /// <summary>
    ///     The Azure AD tenant ID or domain name. Required when using AadSingleOrg account type.
    /// </summary>
    public string? IdentityTenant { get; set; }

    /// <summary>
    ///     The privacy statement URL or text displayed to the user.
    /// </summary>
    public string? PrivacyStatement { get; set; }
}
