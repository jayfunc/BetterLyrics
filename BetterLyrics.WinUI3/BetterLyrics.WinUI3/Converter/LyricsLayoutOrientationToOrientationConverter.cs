using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsLayoutOrientationToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LyricsLayoutOrientation orientation)
            {
                return orientation.ToOrientation();
            }
            return Orientation.Horizontal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
