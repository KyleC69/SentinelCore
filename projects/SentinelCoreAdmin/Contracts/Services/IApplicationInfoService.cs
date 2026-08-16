// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IApplicationInfoService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCoreAdmin.Contracts.Services;





public interface IApplicationInfoService
{
    Version GetVersion();
}