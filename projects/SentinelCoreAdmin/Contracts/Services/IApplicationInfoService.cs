// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IApplicationInfoService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Contracts.Services;





public interface IApplicationInfoService
{
    Version GetVersion();
}