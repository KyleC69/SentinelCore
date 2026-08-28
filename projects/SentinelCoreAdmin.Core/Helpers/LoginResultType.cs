// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         LoginResultType.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Core.Helpers;





public enum LoginResultType
{
    Success, Unauthorized, CancelledByUser, NoNetworkAvailable, UnknownError
}