using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class SecondsToFormattedTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double seconds)
            {
                return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
            }
            else if (value is int secondsInt)
            {
                return TimeSpan.FromSeconds(secondsInt).ToString(@"mm\:ss");
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
