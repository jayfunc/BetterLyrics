using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public class FPSToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int fps)
            {
                return TimeSpan.FromSeconds(1.0 / fps);
            }
            return TimeSpan.FromSeconds(1.0 / 60);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
