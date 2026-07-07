using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class SecondsToFormattedTimeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var timeSpan = TimeSpan.Zero;
            if (value is double seconds)
                timeSpan = TimeSpan.FromSeconds(seconds);
            else if (value is int secondsInt) timeSpan = TimeSpan.FromSeconds(secondsInt);
            if (timeSpan.Days > 0) return timeSpan.ToString(@"dd\.hh\:mm\:ss");

            if (timeSpan.Hours > 0) return timeSpan.ToString(@"hh\:mm\:ss");

            return timeSpan.ToString(@"mm\:ss");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
