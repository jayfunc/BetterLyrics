using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

public partial class GsmtcService : IGsmtcService
{
    private readonly Debouncer _albumArtDebouncer = new();
    private readonly ISystemUIProvider _systemUIProvider = Ioc.Default.GetService<ISystemUIProvider>();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial byte[]? AlbumArtBytes { get; set; }

    public async Task<NowPlayingPalette> CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus lyricsWindowStatus,
        AppColor backdropAccentColor, CancellationToken token = default)
    {
        var accentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
        var lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
        var darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

        accentColors =
            (await PaletteHelper.GetAccentColorsAsync(AlbumArtBytes, 4, lyricsWindowStatus.PaletteGeneratorType, null, lyricsWindowStatus.PaletteChromaWeight, lyricsWindowStatus.PaletteToneWeight, lyricsWindowStatus.PalettePopulationWeight))
            .Select(ColorHelper.FromVector3).ToList();
        token.ThrowIfCancellationRequested();

        lightAccentColors =
            (await PaletteHelper.GetAccentColorsAsync(AlbumArtBytes, 4, lyricsWindowStatus.PaletteGeneratorType, false, lyricsWindowStatus.PaletteChromaWeight, lyricsWindowStatus.PaletteToneWeight, lyricsWindowStatus.PalettePopulationWeight))
            .Select(ColorHelper.FromVector3).ToList();
        token.ThrowIfCancellationRequested();

        darkAccentColors =
            (await PaletteHelper.GetAccentColorsAsync(AlbumArtBytes, 4, lyricsWindowStatus.PaletteGeneratorType, true, lyricsWindowStatus.PaletteChromaWeight, lyricsWindowStatus.PaletteToneWeight, lyricsWindowStatus.PalettePopulationWeight))
            .Select(ColorHelper.FromVector3).ToList();
        token.ThrowIfCancellationRequested();

        var result = new NowPlayingPalette
        {
            UnderlayColor = backdropAccentColor
        };

        AppTheme themeTypeSent;
        if (lyricsWindowStatus.IsAdaptToEnvironment)
            themeTypeSent = ColorHelper.GetElementThemeFromBackgroundColor(result.UnderlayColor);
        else if (lyricsWindowStatus.IsAdaptToAlbumArt)
            themeTypeSent = ColorHelper.GetElementThemeFromBackgroundColor(accentColors.First());
        else
            themeTypeSent = lyricsWindowStatus.WindowTheme;

        var isLight = themeTypeSent switch
        {
            AppTheme.Default => _systemUIProvider.GetAppTheme() == AppTheme.Light,
            AppTheme.Light => true,
            _ => false
        };

        AppColor adaptiveGrayedFontColor;
        AppColor grayedEnvironmentalColor;
        AppColor? adaptiveColoredFontColor;

        var darkColor = Colors.Black;
        var lightColor = Colors.White;

        if (isLight)
        {
            adaptiveGrayedFontColor = darkColor;
            // brightness = 0.7f;
            grayedEnvironmentalColor = lightColor;

            result.AccentColor1 = lightAccentColors.ElementAtOrDefault(0);
            result.AccentColor2 = lightAccentColors.ElementAtOrDefault(1);
            result.AccentColor3 = lightAccentColors.ElementAtOrDefault(2);
            result.AccentColor4 = lightAccentColors.ElementAtOrDefault(3);
        }
        else
        {
            adaptiveGrayedFontColor = lightColor;
            // brightness = 0.3f;
            grayedEnvironmentalColor = darkColor;

            result.AccentColor1 = darkAccentColors.ElementAtOrDefault(0);
            result.AccentColor2 = darkAccentColors.ElementAtOrDefault(1);
            result.AccentColor3 = darkAccentColors.ElementAtOrDefault(2);
            result.AccentColor4 = darkAccentColors.ElementAtOrDefault(3);
        }

        if (lyricsWindowStatus.IsAdaptToEnvironment)
        {
            adaptiveColoredFontColor = ColorHelper.GetForegroundColor(result.UnderlayColor);
        }
        else
        {
            if (isLight)
                adaptiveColoredFontColor = darkAccentColors.ElementAtOrDefault(0);
            else
                adaptiveColoredFontColor = lightAccentColors.ElementAtOrDefault(0);
        }

        result.ThemeType = themeTypeSent;

        // 背景字色
        result.NonCurrentLineFillColor = lyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType switch
        {
            LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
            LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
            LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor,
            _ => adaptiveGrayedFontColor
        };

        // 频谱填充色
        result.SpectrumColor = lyricsWindowStatus.LyricsBackgroundSettings.SpectrumColorType switch
        {
            LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
            LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
            LyricsFontColorType.Custom => lyricsWindowStatus.LyricsBackgroundSettings.SpectrumCustomColor,
            _ => adaptiveGrayedFontColor
        };

        // 前景字色
        result.PlayedCurrentLineFillColor = lyricsWindowStatus.LyricsStyleSettings.LyricsPlayedFgFontColorType switch
        {
            LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
            LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
            LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedFgFontColor,
            _ => adaptiveGrayedFontColor
        };
        result.UnplayedCurrentLineFillColor =
            lyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedFgFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedFgFontColor,
                _ => adaptiveGrayedFontColor
            };

        // 描边颜色
        result.PlayedTextStrokeColor = lyricsWindowStatus.LyricsStyleSettings.LyricsPlayedStrokeFontColorType switch
        {
            LyricsFontColorType.AdaptiveGrayed => grayedEnvironmentalColor.WithBrightness(0.7),
            LyricsFontColorType.AdaptiveColored => result.UnderlayColor.WithBrightness(0.7),
            LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedStrokeFontColor,
            _ => Colors.Transparent
        };
        result.UnplayedTextStrokeColor = lyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedStrokeFontColorType switch
        {
            LyricsFontColorType.AdaptiveGrayed => grayedEnvironmentalColor.WithBrightness(0.7),
            LyricsFontColorType.AdaptiveColored => result.UnderlayColor.WithBrightness(0.7),
            LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedStrokeFontColor,
            _ => Colors.Transparent
        };
        return result;
    }

    public async Task<List<AppColor>> GetAlbumArtAccentColorsAsync(PaletteGeneratorType paletteGeneratorType,
        bool isDark, CancellationToken token = default)
    {
        var lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
        var darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

        lightAccentColors =
            (await PaletteHelper.GetAccentColorsAsync(AlbumArtBytes, 4, paletteGeneratorType, false))
            .Select(ColorHelper.FromVector3).ToList();
        token.ThrowIfCancellationRequested();

        darkAccentColors =
            (await PaletteHelper.GetAccentColorsAsync(AlbumArtBytes, 4, paletteGeneratorType, true))
            .Select(ColorHelper.FromVector3).ToList();
        token.ThrowIfCancellationRequested();

        return isDark ? darkAccentColors : lightAccentColors;
    }

    private void UpdateAlbumArt(bool ignoreCache = false)
    {
        _ = _albumArtDebouncer.RunAsync(async token => await RefreshArtAlbumAsync(ignoreCache, token));
    }

    private async Task RefreshArtAlbumAsync(bool ignoreCache, CancellationToken token)
    {
        if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            AlbumArtBytes =
                await Task.Run(
                    async () => await _albumArtSearchService.SearchAsync(CurrentSongInfo, _smtcAlbumArtBuffer,
                        ignoreCache, token), token);
        else
            AlbumArtBytes = null;
    }
}