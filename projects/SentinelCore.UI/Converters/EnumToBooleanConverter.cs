// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         EnumToBooleanConverter.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Globalization;
using System.Windows.Data;




namespace SentinelCore.UI.Converters;





/// <summary>
///     Converts an enum value to a boolean by comparing it to the converter parameter.
///     Used for radio-button binding scenarios where each radio represents one enum member.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public Type? EnumType { get; set; }








    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(culture);
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








    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(EnumToBooleanConverter)} is a one-way converter.");
    }
}
