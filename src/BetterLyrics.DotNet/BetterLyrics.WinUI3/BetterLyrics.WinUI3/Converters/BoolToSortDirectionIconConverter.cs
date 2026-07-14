using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters;

public class BoolToSortDirectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isDescending)
        {
            return isDescending ? "\uE74B" : "\uE74A";
        }
        return "\uE74A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
