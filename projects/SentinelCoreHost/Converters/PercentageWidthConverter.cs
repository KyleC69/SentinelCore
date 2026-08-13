// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         PercentageWidthConverter.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Globalization;
using System.Windows.Data;




namespace SentinelCoreHost.Converters;





/// <summary>
///     Converts an ancestor element's ActualWidth to a percentage of that width.
///     The converter parameter specifies the percentage as a decimal (e.g., 0.8).
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public sealed class PercentageWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width && double.IsFinite(width) && width > 0)
        {
            double factor = parameter is string s && double.TryParse(s, NumberStyles.Any, culture, out double parsed) ? parsed : parameter as double? ?? 1.0;

            return Math.Max(0, width * factor);
        }

        return double.NaN;
    }








    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}