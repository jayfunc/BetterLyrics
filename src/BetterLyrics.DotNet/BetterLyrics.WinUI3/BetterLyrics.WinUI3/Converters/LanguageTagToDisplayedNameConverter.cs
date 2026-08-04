using BetterLyrics.Core.Models;
using Microsoft.UI.Xaml.Data;
using NLanguageTag;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class LanguageTagToDisplayedNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LanguageTag langTag)
            {
                var langCode = langTag.ToString();
                return new ExtendedLanguage(langCode).DisplayName ?? langCode;
            }

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
