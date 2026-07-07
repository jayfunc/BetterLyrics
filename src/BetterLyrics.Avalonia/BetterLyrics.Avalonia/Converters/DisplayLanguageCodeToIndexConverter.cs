using Avalonia.Data.Converters;
using BetterLyrics.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class DisplayLanguageCodeToIndexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string langCode)
            {
                var found = LanguageHelper.SupportedDisplayLanguages.FindIndex(x => x.LanguageCode == langCode);
                return found == -1 ? 0 : found;
            }

            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int index) return LanguageHelper.SupportedDisplayLanguages.ElementAt(index).LanguageCode;
            return "";
        }
    }
}
