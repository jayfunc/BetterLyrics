using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public class LyricsSearchProviderToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LyricsSearchProvider provider)
            {
                return provider switch
                {
                    LyricsSearchProvider.LocalLrcFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLocalLrcFile"
                    ),
                    LyricsSearchProvider.LocalMusicFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLocalMusicFile"
                    ),
                    LyricsSearchProvider.LrcLib => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLrcLib"
                    ),
                    LyricsSearchProvider.QQMusic => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderQQMusic"
                    ),
                    LyricsSearchProvider.KugouMusic => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderKugouMusic"
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
