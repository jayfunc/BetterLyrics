using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Extensions;

public static class LyricsLayerTypeExtensions
{
    private static readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    extension(LyricsLayerType type)
    {
        public string ToDisplayName()
        {
            return type switch
            {
                LyricsLayerType.Primary => _localizationService.GetLocalizedString("LyricsLayerPrimaryName"),
                LyricsLayerType.Secondary => _localizationService.GetLocalizedString("LyricsLayerSecondaryName"),
                LyricsLayerType.Tertiary => _localizationService.GetLocalizedString("LyricsLayerTertiaryName"),
                _ => ""
            };
        }
    }
}