using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public class MillisecondsToFormattedTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int milliseconds)
            {
                return TimeSpan.FromMilliseconds(milliseconds).ToString(@"mm\:ss\.fff");
            }
            else if (value is double doubleMilliseconds)
            {
                return TimeSpan.FromMilliseconds(doubleMilliseconds).ToString(@"mm\:ss\.fff");
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
