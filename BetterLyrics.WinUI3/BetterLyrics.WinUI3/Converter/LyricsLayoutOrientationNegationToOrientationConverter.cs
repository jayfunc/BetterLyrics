using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsLayoutOrientationNegationToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LyricsLayoutOrientation orientation)
            {
                return orientation.ToOrientationInverse();
            }
            return Orientation.Horizontal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
