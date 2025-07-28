// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class TranslationSearchProviderToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TranslationSearchProvider provider)
            {
                return provider switch
                {
                    TranslationSearchProvider.LrcLib => "LrcLib",
                    TranslationSearchProvider.QQ => "QQ",
                    TranslationSearchProvider.Netease => "Netease",
                    TranslationSearchProvider.Kugou => "Kugou",
                    TranslationSearchProvider.AmllTtmlDb => "amll-ttml-db",
                    TranslationSearchProvider.LocalLrcFile => App.ResourceLoader!.GetString("LyricsSearchProviderLocalLrcFile"),
                    TranslationSearchProvider.LocalMusicFile => App.ResourceLoader!.GetString("LyricsSearchProviderLocalMusicFile"),
                    TranslationSearchProvider.LocalEslrcFile => App.ResourceLoader!.GetString("LyricsSearchProviderEslrcFile"),
                    TranslationSearchProvider.LocalTtmlFile => App.ResourceLoader!.GetString("LyricsSearchProviderTtmlFile"),
                    TranslationSearchProvider.LibreTranslate => "LibreTranslate",
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
