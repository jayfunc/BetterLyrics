using System;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class TransliterationSearchProviderToDisplayNameConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
    private readonly IPluginService _pluginService = Ioc.Default.GetRequiredService<IPluginService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TransliterationSearchProvider provider)
            return provider switch
            {
                TransliterationSearchProvider.LrcLib => "LrcLib",
                TransliterationSearchProvider.QQ => "QQ 音乐",
                TransliterationSearchProvider.Netease => "网易云音乐",
                TransliterationSearchProvider.Kugou => "酷狗音乐",
                TransliterationSearchProvider.AmllTtmlDb => "amll-ttml-db",
                TransliterationSearchProvider.AppleMusic => "Apple Music",
                TransliterationSearchProvider.LocalLrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalLrcFile"),
                TransliterationSearchProvider.LocalMusicFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalMusicFile"),
                TransliterationSearchProvider.LocalEslrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderEslrcFile"),
                TransliterationSearchProvider.LocalTtmlFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderTtmlFile"),
                TransliterationSearchProvider.BetterLyrics => "BetterLyrics",
                _ => _pluginService.GetPluginId((int)provider)
            };
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}