using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public class ShortcutToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is List<string> shortcut) return string.Join(" + ", shortcut);
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}