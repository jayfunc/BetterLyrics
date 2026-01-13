using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class DoubleToDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return string.Empty;

            if (double.TryParse(value.ToString(), out double number))
            {
                int decimalPlaces = 2;
                if (parameter != null && int.TryParse(parameter.ToString(), out int parsedParams))
                {
                    decimalPlaces = parsedParams;
                }

                return number.ToString($"F{decimalPlaces}");
            }

            return value.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
