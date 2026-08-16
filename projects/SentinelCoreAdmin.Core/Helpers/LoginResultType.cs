// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         LoginResultType.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCoreAdmin.Core.Helpers;





public enum LoginResultType
{
    Success, Unauthorized, CancelledByUser, NoNetworkAvailable, UnknownError
}