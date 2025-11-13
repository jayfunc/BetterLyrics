using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using System;
using System.Linq;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class DisplayLanguageCodeToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string langCode)
            {
                return LanguageHelper.SupportedDisplayLanguages.FindIndex(x => x.LanguageCode == langCode);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int index)
            {
                return LanguageHelper.SupportedDisplayLanguages.ElementAt(index).LanguageCode;
            }
            return "";
        }
    }
}
