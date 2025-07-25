// 2025/6/23 by Zhe Fang

using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Enum)
            {
                return System.Convert.ToInt32(value);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int && targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }
            return Enum.ToObject(targetType, 0);
        }
    }
}
