// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         IIdentityCacheService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Core.Contracts.Services;





public interface IIdentityCacheService
{

    byte[]? ReadMsalToken();


    void SaveMsalToken(byte[] token);
}