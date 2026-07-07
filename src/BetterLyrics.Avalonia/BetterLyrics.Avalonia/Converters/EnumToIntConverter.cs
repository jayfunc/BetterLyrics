using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace BetterLyrics.Avalonia.Converters;

public class EnumToIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            return System.Convert.ToInt32(enumValue);
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && targetType.IsEnum)
        {
            return Enum.ToObject(targetType, intValue);
        }

        return BindingOperations.DoNothing;
    }
}