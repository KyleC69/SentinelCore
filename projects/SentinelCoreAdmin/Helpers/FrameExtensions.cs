// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         FrameExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows;
using System.Windows.Controls;

using JetBrains.Annotations;




namespace SentinelCoreAdmin.Helpers;





public static class FrameExtensions
{

    public static void CleanNavigation([NotNull] this Frame frame)
    {
        while (frame.CanGoBack)
        {
            frame.RemoveBackEntry();
        }
    }








    [CanBeNull]
    public static object? GetDataContext([NotNull] this Frame frame)
    {
        if (frame.Content is FrameworkElement element)
        {
            return element.DataContext;
        }

        return null;
    }
}