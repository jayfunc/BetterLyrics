using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LanguageCodeToDisplayedNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string langCode)
            {
                if (langCode == "N/A")
                {
                    return langCode;
                }
                else if (PhoneticHelper.IsPhoneticCode(langCode))
                {
                    return PhoneticHelper.GetDisplayName(langCode);
                }
                else
                {
                    return new Language(langCode).DisplayName ?? langCode;
                }
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
