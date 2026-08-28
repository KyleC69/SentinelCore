// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IUserDataService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IUserDataService
{

    UserViewModel? GetUser();


    void Initialize();


    event EventHandler<UserViewModel>? UserDataUpdated;
}