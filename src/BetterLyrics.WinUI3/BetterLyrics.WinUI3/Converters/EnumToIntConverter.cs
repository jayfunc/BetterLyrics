// 2025/6/23 by Zhe Fang

using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class EnumToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue)
        {
            var values = Enum.GetValues(enumValue.GetType());
            return Array.IndexOf(values, enumValue);
        }

        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int index && targetType.IsEnum)
        {
            var values = Enum.GetValues(targetType);

            if (index >= 0 && index < values.Length) return values.GetValue(index);
        }

        return Enum.GetValues(targetType).GetValue(0);
    }
}