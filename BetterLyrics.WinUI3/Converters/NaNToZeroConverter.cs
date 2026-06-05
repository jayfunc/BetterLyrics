using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Converters
{
    public class NaNToZeroConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(double))
            {
                return double.TryParse(value?.ToString(), out var result) ? result : 0.0;
            }
            else if (targetType == typeof(int))
            {
                return int.TryParse(value?.ToString(), out var result) ? result : 0;
            }
            else
            {
                return value;
            }
        }
    }
}
