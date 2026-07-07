using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public class BoolToPartialOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue) return boolValue ? 1.0 : 0.7;
        return 1.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}