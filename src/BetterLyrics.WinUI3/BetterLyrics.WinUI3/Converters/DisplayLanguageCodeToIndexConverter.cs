using System;
using System.Linq;
using BetterLyrics.Core.Helpers;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class DisplayLanguageCodeToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string langCode)
        {
            var found = LanguageHelper.SupportedDisplayLanguages.FindIndex(x => x.LanguageCode == langCode);
            return found == -1 ? 0 : found;
        }

        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int index) return LanguageHelper.SupportedDisplayLanguages.ElementAt(index).LanguageCode;
        return "";
    }
}