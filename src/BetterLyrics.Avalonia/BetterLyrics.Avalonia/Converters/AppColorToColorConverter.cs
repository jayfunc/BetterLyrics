using Avalonia.Data.Converters;
using Avalonia.Media;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class AppColorToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AppColor appColor) return ColorExtensions.FromAppColor(appColor);
            return Core.Constants.Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color) return color.ToAppColor();
            return Core.Constants.Colors.Transparent;
        }
    }
}
