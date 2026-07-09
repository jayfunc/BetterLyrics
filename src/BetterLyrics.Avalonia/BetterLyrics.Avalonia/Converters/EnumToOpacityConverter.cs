using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class EnumToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return 0.0;

            if (IsMatch(value, parameter)) return 1.0;

            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private bool IsMatch(object value, object parameter)
        {
            string? valueString;

            if (value.GetType().IsEnum)
                valueString = ((int)value).ToString();
            else
                valueString = value.ToString();

            return valueString == parameter.ToString();
        }
    }
}
