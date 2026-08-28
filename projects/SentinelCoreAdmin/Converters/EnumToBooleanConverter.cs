// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         EnumToBooleanConverter.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Globalization;
using System.Windows.Data;

using JetBrains.Annotations;




namespace SentinelCoreAdmin.Converters;





public class EnumToBooleanConverter : IValueConverter
{
    public Type? EnumType { get; set; }








    public object Convert(object value, [CanBeNull] Type? targetType, object parameter, [CanBeNull] CultureInfo? culture)
    {
        if (EnumType != null && parameter is string enumString)
        {
            if (Enum.IsDefined(EnumType, value))
            {
                object enumValue = Enum.Parse(EnumType, enumString);

                return enumValue.Equals(value);
            }
        }

        return false;
    }








    public object? ConvertBack(object value, [CanBeNull] Type? targetType, object parameter, [CanBeNull] CultureInfo? culture)
    {
        if (EnumType != null && parameter is string enumString)
        {
            return Enum.Parse(EnumType, enumString);
        }

        return null;
    }
}