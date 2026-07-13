using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class SecondsToFormattedTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var timeSpan = TimeSpan.Zero;
        if (value is double seconds)
            timeSpan = TimeSpan.FromSeconds(seconds);
        else if (value is int secondsInt) timeSpan = TimeSpan.FromSeconds(secondsInt);
        if (timeSpan.Days > 0) return timeSpan.ToString(@"dd\.hh\:mm\:ss");

        if (timeSpan.Hours > 0) return timeSpan.ToString(@"hh\:mm\:ss");

        return timeSpan.ToString(@"mm\:ss");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}