using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public class RectToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Windows.Foundation.Rect rect)
            {
                return new Microsoft.UI.Xaml.Thickness(rect.X, rect.Y, 0, 0);
            }
            return new Microsoft.UI.Xaml.Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
