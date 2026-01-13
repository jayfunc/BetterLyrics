using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class SecondsToFormattedTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            TimeSpan timeSpan = TimeSpan.Zero;
            if (value is double seconds)
            {
                timeSpan = TimeSpan.FromSeconds(seconds);
            }
            else if (value is int secondsInt)
            {
                timeSpan = TimeSpan.FromSeconds(secondsInt);
            }
            if (timeSpan.Days > 0)
            {
                return timeSpan.ToString(@"dd\.hh\:mm\:ss");
            }
            else if (timeSpan.Hours > 0)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return timeSpan.ToString(@"mm\:ss");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
