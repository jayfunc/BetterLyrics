using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class TransliterationSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TransliterationSearchProvider provider)
            {
                return provider switch
                {
                    TransliterationSearchProvider.LrcLib => "LrcLib",
                    TransliterationSearchProvider.QQ => "QQ 音乐",
                    TransliterationSearchProvider.Netease => "网易云音乐",
                    TransliterationSearchProvider.Kugou => "酷狗音乐",
                    TransliterationSearchProvider.AmllTtmlDb => "amll-ttml-db",
                    TransliterationSearchProvider.AppleMusic => "Apple Music",
                    TransliterationSearchProvider.LocalLrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalLrcFile"),
                    TransliterationSearchProvider.LocalMusicFile => _resourceService.GetLocalizedString("LyricsSearchProviderLocalMusicFile"),
                    TransliterationSearchProvider.LocalEslrcFile => _resourceService.GetLocalizedString("LyricsSearchProviderEslrcFile"),
                    TransliterationSearchProvider.LocalTtmlFile => _resourceService.GetLocalizedString("LyricsSearchProviderTtmlFile"),
                    TransliterationSearchProvider.BetterLyrics => "BetterLyrics",
                    TransliterationSearchProvider.CutletDocker => "cutlet-docker",
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
