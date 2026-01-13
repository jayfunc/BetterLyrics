using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class UriStringToDecodedAbsoluteUri : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string uriString)
            {
                return uriString.ToDecodedAbsoluteUri();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
