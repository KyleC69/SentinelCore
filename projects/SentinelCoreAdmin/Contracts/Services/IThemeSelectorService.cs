// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IThemeSelectorService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCoreAdmin.Models;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IThemeSelectorService
{

    AppTheme GetCurrentTheme();


    void InitializeTheme();


    void SetTheme(AppTheme theme);
}