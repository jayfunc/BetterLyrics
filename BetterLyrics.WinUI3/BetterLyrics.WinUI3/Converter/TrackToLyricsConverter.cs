using ATL;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class TrackToLyricsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Track track)
            {
                return track.GetLyrics();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
