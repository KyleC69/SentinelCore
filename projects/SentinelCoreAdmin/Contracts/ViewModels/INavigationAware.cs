// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         INavigationAware.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Contracts.ViewModels;





public interface INavigationAware
{

    void OnNavigatedFrom();


    void OnNavigatedTo(object parameter);
}