// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         InvertBoolToVisibilityConverter.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Globalization;
using System.Windows;
using System.Windows.Data;




namespace SentinelCore.UI.Converters;





/// <summary>
///     Converts a boolean to <see cref="Visibility" />, inverting the result.
///     <c>true</c> → <see cref="Visibility.Collapsed" />,
///     <c>false</c> → <see cref="Visibility.Visible" />.
/// </summary>
public sealed class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        bool flag = value is bool b && b;
        return flag ? Visibility.Collapsed : Visibility.Visible;
    }








    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible ? false : true;
    }
}
