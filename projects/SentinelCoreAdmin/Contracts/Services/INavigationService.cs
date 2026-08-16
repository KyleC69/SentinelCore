// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         INavigationService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;




namespace SentinelCoreAdmin.Contracts.Services;





public interface INavigationService
{

    bool CanGoBack { get; }


    void CleanNavigation();


    void GoBack();


    void Initialize(Frame shellFrame);


    bool NavigateTo(string pageKey, object parameter = null, bool clearNavigation = false);


    event EventHandler<string> Navigated;


    void UnsubscribeNavigation();
}