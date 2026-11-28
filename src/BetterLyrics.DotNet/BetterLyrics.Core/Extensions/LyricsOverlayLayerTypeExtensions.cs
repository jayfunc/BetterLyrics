using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Extensions;

public static class LyricsOverlayLayerTypeExtensions
{
    private static readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    extension(LyricsOverlayLayerType type)
    {
        public string ToDisplayName()
        {
            return type switch
            {
                LyricsOverlayLayerType.PureColor => _localizationService.GetLocalizedString(
                    "SettingsPagePureLayer/Header"),
                LyricsOverlayLayerType.DesktopWallpaper => _localizationService.GetLocalizedString(
                    "SettingsPageDesktopWallpaperLayer/Header"),
                LyricsOverlayLayerType.CoverOverlay => _localizationService.GetLocalizedString(
                    "SettingsPageAlbumArtLayer/Header"),
                LyricsOverlayLayerType.Fluid => _localizationService.GetLocalizedString(
                    "SettingsPageFluidLayer/Header"),
                LyricsOverlayLayerType.Spectrum => _localizationService.GetLocalizedString(
                    "SettingsPageSpectrumLayer/Header"),
                LyricsOverlayLayerType.SnowFlake => _localizationService.GetLocalizedString(
                    "SettingsPageSnowFlakeLayer/Header"),
                LyricsOverlayLayerType.Fog => _localizationService.GetLocalizedString(
                    "SettingsPageFogLayer/Header"),
                LyricsOverlayLayerType.Raindrop => _localizationService.GetLocalizedString(
                    "SettingsPageRaindropLayer/Header"),
                LyricsOverlayLayerType.SemanticEffect => _localizationService.GetLocalizedString(
                    "SettingsPageSemanticEffects/Header"),
                LyricsOverlayLayerType.Lyrics => _localizationService.GetLocalizedString(
                    "LyricsOverlayLayerLyricsName"),
                LyricsOverlayLayerType.AlbumArt => _localizationService.GetLocalizedString(
                    "SettingsPageAlbumArt/Text"),
                LyricsOverlayLayerType.SongInfo => _localizationService.GetLocalizedString(
                    "SettingsPageSongInfo/Text"),
                _ => ""
            };
        }
    }
}