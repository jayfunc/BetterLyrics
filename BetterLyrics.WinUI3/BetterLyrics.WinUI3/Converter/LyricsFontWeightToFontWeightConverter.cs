using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsFontWeightToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LyricsFontWeight weight)
            {
                return weight.ToFontWeight();
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
