using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public class NaNToZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (targetType == typeof(double)) return double.TryParse(value?.ToString(), out var result) ? result : 0.0;

        if (targetType == typeof(int)) return int.TryParse(value?.ToString(), out var result) ? result : 0;

        return value;
    }
}