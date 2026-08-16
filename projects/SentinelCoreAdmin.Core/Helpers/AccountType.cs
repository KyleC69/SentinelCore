// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         AccountType.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCoreAdmin.Core.Helpers;





/// <summary>
///     Represents the type of Microsoft identity account the user chooses to sign in with.
/// </summary>
public enum AccountType
{
    /// <summary>Azure AD and personal Microsoft accounts.</summary>
    AadAndPersonalMsAccounts,

    /// <summary>Personal Microsoft accounts only.</summary>
    PersonalMsAccounts,

    /// <summary>Azure AD multiple organizations.</summary>
    AadMultipleOrgs,

    /// <summary>Azure AD single organization (requires tenant ID).</summary>
    AadSingleOrg
}