// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IWindowManagerService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IWindowManagerService
{
    Window MainWindow { get; }


    Window GetWindow(string pageKey);


    bool? OpenInDialog(string pageKey, object parameter = null);


    void OpenInNewWindow(string pageKey, object parameter = null);
}