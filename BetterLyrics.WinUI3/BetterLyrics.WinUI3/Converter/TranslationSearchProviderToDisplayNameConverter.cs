// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class TranslationSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TranslationSearchProvider provider)
            {
                return provider switch
                {
                    TranslationSearchProvider.LrcLib => "LrcLib",
                    TranslationSearchProvider.QQ => "QQ 音乐",
                    TranslationSearchProvider.Netease => "网易云音乐",
                    TranslationSearchProvider.Kugou => "酷狗音乐",
                    TranslationSearchProvider.AmllTtmlDb => "amll-ttml-db",
                    TranslationSearchProvider.AppleMusic => "Apple Music",
                    TranslationSearchProvider.LocalLrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalLrcFile"),
                    TranslationSearchProvider.LocalMusicFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalMusicFile"),
                    TranslationSearchProvider.LocalEslrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderEslrcFile"),
                    TranslationSearchProvider.LocalTtmlFile => _resourceService.GetLocalizedString("LyricsSearchProviderTtmlFile"),
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
