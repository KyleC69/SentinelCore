// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IShellDialogWindow.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;




namespace SentinelCoreAdmin.Contracts.Views;





public interface IShellDialogWindow
{
    Frame GetDialogFrame();
}