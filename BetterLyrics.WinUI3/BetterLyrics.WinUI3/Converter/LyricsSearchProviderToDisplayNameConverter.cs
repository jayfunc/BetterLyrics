// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class LyricsSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

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
                    LyricsSearchProvider.LocalLrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalLrcFile"),
                    LyricsSearchProvider.LocalMusicFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalMusicFile"),
                    LyricsSearchProvider.LocalEslrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderEslrcFile"),
                    LyricsSearchProvider.LocalTtmlFile => _resourceService.GetLocalizedString("LyricsSearchProviderTtmlFile"),
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
