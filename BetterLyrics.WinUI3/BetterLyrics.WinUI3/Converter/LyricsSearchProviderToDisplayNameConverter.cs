// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsSearchProviderToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LyricsSearchProvider provider)
            {
                return provider switch
                {
                    LyricsSearchProvider.LrcLib => "LrcLib",
                    LyricsSearchProvider.QQ => "QQ 音乐",
                    LyricsSearchProvider.Netease => "网易云音乐",
                    LyricsSearchProvider.Kugou => "酷狗音乐",
                    LyricsSearchProvider.AmllTtmlDb => "amll-ttml-db",
                    LyricsSearchProvider.LocalLrcFile => App.ResourceLoader!.GetString("LyricsSearchProviderLocalLrcFile"),
                    LyricsSearchProvider.LocalMusicFile => App.ResourceLoader!.GetString("LyricsSearchProviderLocalMusicFile"),
                    LyricsSearchProvider.LocalEslrcFile => App.ResourceLoader!.GetString("LyricsSearchProviderEslrcFile"),
                    LyricsSearchProvider.LocalTtmlFile => App.ResourceLoader!.GetString("LyricsSearchProviderTtmlFile"),
                    _ => "N/A",
                };
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
