using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class CornerRadiusToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Microsoft.UI.Xaml.CornerRadius cornerRadius)
            {
                return (double)cornerRadius.TopLeft;
            }
            return .0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
