// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         IMicrosoftGraphService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCoreAdmin.Core.Models;




namespace SentinelCoreAdmin.Core.Contracts.Services;





public interface IMicrosoftGraphService
{
    Task<User?> GetUserInfoAsync(string accessToken);


    Task<string> GetUserPhoto(string accessToken);
}