// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IShellWindow.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;

using MahApps.Metro.Controls;




namespace SentinelCoreAdmin.Contracts.Views;





public interface IShellWindow
{

    void CloseWindow();


    Frame GetNavigationFrame();


    Frame GetRightPaneFrame();


    SplitView GetSplitView();


    void ShowWindow();
}