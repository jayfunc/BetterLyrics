using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public class FPSToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is FPS fps) return TimeSpan.FromSeconds(1.0 / (int)fps);
        return TimeSpan.FromSeconds(1.0 / 60);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}