using Avalonia.Data.Converters;
using Avalonia.Media;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using System;
using System.Globalization;

namespace BetterLyrics.Avalonia.Converters
{
    public class LyricsFontWeightToFontWeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LyricsFontWeight weight) return weight.ToFontWeight();
            return FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
