// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         WindowExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows;
using System.Windows.Controls;

using JetBrains.Annotations;




namespace SentinelCoreAdmin.Helpers;





public static class WindowExtensions
{
    [CanBeNull]
    public static object GetDataContext([NotNull] this Window window)
    {
        if (window.Content is Frame frame)
        {
            return frame.GetDataContext();
        }

        return null;
    }
}