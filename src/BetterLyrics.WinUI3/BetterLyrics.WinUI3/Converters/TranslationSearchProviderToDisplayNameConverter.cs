// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class TranslationSearchProviderToDisplayNameConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
    private readonly IPluginService _pluginService = Ioc.Default.GetRequiredService<IPluginService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TranslationSearchProvider provider)
            return provider switch
            {
                TranslationSearchProvider.LrcLib => "LrcLib",
                TranslationSearchProvider.QQ => "QQ 音乐",
                TranslationSearchProvider.Netease => "网易云音乐",
                TranslationSearchProvider.Kugou => "酷狗音乐",
                TranslationSearchProvider.AmllTtmlDb => "amll-ttml-db",
                TranslationSearchProvider.AppleMusic => "Apple Music",
                TranslationSearchProvider.LocalLrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalLrcFile"),
                TranslationSearchProvider.LocalMusicFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalMusicFile"),
                TranslationSearchProvider.LocalEslrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderEslrcFile"),
                TranslationSearchProvider.LocalTtmlFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderTtmlFile"),
                TranslationSearchProvider.LibreTranslate => "LibreTranslate",
                _ => _pluginService.GetPluginId((int)provider)
            };
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}