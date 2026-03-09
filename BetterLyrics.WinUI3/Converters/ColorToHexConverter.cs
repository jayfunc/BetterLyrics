using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class ColorToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Color color)
            {
                return color.ToHex();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
