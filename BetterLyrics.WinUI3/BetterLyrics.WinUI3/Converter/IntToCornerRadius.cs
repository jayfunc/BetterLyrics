// 2025/6/23 by Zhe Fang

using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class IntToCornerRadius : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue && parameter is double controlHeight)
            {
                return new Microsoft.UI.Xaml.CornerRadius(intValue / 100f * controlHeight / 2);
            }
            return new Microsoft.UI.Xaml.CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
