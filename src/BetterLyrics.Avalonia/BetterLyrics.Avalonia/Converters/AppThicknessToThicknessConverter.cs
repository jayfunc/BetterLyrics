using Avalonia;
using Avalonia.Data.Converters;
using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class AppThicknessToThicknessConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AppThickness appThickness)
                return new Thickness(appThickness.Left, appThickness.Top, appThickness.Right, appThickness.Bottom);
            return new Thickness(0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
