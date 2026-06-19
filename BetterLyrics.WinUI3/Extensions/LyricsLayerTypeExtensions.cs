using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsLayerTypeExtensions
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        extension(LyricsLayerType type)
        {
            public string ToDisplayName() => type switch
            {
                LyricsLayerType.Primary => _localizationService.GetLocalizedString("LyricsLayerPrimaryName"),
                LyricsLayerType.Secondary => _localizationService.GetLocalizedString("LyricsLayerSecondaryName"),
                LyricsLayerType.Tertiary => _localizationService.GetLocalizedString("LyricsLayerTertiaryName"),
                _ => ""
            };
        }
    }
}
