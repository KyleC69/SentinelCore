// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IRightPaneService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;

using MahApps.Metro.Controls;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IRightPaneService
{

    void CleanUp();


    void Initialize(Frame rightPaneFrame, SplitView splitView);


    void OpenInRightPane(string pageKey, object parameter = null);


    event EventHandler PaneClosed;
    event EventHandler PaneOpened;
}