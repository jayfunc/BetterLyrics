// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly ILocalizer _localizer = Localizer.Get();

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
                    LyricsSearchProvider.AppleMusic => "Apple Music",
                    LyricsSearchProvider.LocalLrcFile => _localizer.GetLocalizedString("LyricsSearchProviderLocalLrcFile"),
                    LyricsSearchProvider.LocalMusicFile => _localizer.GetLocalizedString("LyricsSearchProviderLocalMusicFile"),
                    LyricsSearchProvider.LocalEslrcFile => _localizer.GetLocalizedString("LyricsSearchProviderEslrcFile"),
                    LyricsSearchProvider.LocalTtmlFile => _localizer.GetLocalizedString("LyricsSearchProviderTtmlFile"),
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
