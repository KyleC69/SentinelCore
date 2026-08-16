// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         INavigationAware.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCoreAdmin.Contracts.ViewModels;





public interface INavigationAware
{

    void OnNavigatedFrom();


    void OnNavigatedTo(object parameter);
}