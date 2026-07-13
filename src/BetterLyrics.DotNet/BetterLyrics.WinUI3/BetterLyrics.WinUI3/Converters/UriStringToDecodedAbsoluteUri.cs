using System;
using BetterLyrics.Core.Extensions;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class UriStringToDecodedAbsoluteUri : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string uriString) return uriString.ToDecodedAbsoluteUri();

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}