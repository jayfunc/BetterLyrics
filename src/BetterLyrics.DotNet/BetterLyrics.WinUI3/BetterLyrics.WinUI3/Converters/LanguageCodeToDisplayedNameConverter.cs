using System;
using Windows.Globalization;
using BetterLyrics.Core.Helpers;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class LanguageCodeToDisplayedNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string langCode)
        {
            if (langCode == "N/A") return langCode;

            if (LanguageHelper.IsPhoneticCode(langCode)) return LanguageHelper.GetDisplayName(langCode);

            return new Language(langCode).DisplayName ?? langCode;
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}