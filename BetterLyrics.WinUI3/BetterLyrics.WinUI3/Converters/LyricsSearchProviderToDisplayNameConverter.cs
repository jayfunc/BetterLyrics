// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class LyricsSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

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
                    LyricsSearchProvider.LocalLrcFile => _localizationService.GetLocalizedString("LyricsSearchProviderLocalLrcFile"),
                    LyricsSearchProvider.LocalMusicFile => _localizationService.GetLocalizedString("LyricsSearchProviderLocalMusicFile"),
                    LyricsSearchProvider.LocalEslrcFile => _localizationService.GetLocalizedString("LyricsSearchProviderEslrcFile"),
                    LyricsSearchProvider.LocalTtmlFile => _localizationService.GetLocalizedString("LyricsSearchProviderTtmlFile"),
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
