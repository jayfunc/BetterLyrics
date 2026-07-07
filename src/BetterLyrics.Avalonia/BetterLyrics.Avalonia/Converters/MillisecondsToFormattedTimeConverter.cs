using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class MillisecondsToFormattedTimeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double? milliseconds = null;

            if (value is int iVal) milliseconds = iVal;
            else if (value is double dVal) milliseconds = dVal;
            else if (value is long lVal) milliseconds = lVal;

            if (milliseconds.HasValue)
            {
                var ts = TimeSpan.FromMilliseconds(milliseconds.Value);

                var format = parameter?.ToString();

                if (string.IsNullOrEmpty(format)) format = @"mm\:ss\.fff";

                try
                {
                    return ts.ToString(format);
                }
                catch (FormatException)
                {
                    return ts.ToString();
                }
            }

            return value?.ToString() ?? "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
