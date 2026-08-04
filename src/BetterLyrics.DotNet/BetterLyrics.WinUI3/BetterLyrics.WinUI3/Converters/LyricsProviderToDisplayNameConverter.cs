// 2025/6/23 by Zhe Fang

using System;
using System.Linq;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class LyricsProviderToDisplayNameConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
    private readonly IPluginService _pluginService = Ioc.Default.GetRequiredService<IPluginService>();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LyricsProvider provider)
        {
            if (provider.IsPlugin())
            {
                var plugin = _settingsService.AppSettings.PluginsInfo
                    .FirstOrDefault(x => _pluginService.GetPluginHashedId(x.Id) == (int)provider)?.Plugin;
                if (plugin == null) return "N/A";

                if (string.IsNullOrEmpty(plugin.Title)) return plugin.Id;

                return plugin.Title;
            }

            return provider switch
            {
                LyricsProvider.LrcLib => "LrcLib",
                LyricsProvider.QQ => "QQ 音乐",
                LyricsProvider.Netease => "网易云音乐",
                LyricsProvider.Kugou => "酷狗音乐",
                LyricsProvider.AmllTtmlDb => "amll-ttml-db",
                LyricsProvider.AppleMusic => "Apple Music",
                LyricsProvider.BetterLyrics => "BetterLyrics",
                LyricsProvider.LibreTranslate => "LibreTranslate",
                LyricsProvider.LocalLrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalLrcFile"),
                LyricsProvider.LocalMusicFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderLocalMusicFile"),
                LyricsProvider.LocalEslrcFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderEslrcFile"),
                LyricsProvider.LocalTtmlFile => _localizationService.GetLocalizedString(
                    "LyricsSearchProviderTtmlFile"),
                _ => _pluginService.GetPluginId((int)provider)
            };
        }

        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}