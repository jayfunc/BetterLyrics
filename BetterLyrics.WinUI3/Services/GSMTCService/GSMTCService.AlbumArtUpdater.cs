using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : IGSMTCService
    {
        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();

        private List<Color> _lightAccentColorsMedianCut = Enumerable.Repeat(Colors.Black, 4).ToList();
        private List<Color> _darkAccentColorsMedianCut = Enumerable.Repeat(Colors.Black, 4).ToList();

        private List<Color> _lightAccentColorsOctTree = Enumerable.Repeat(Colors.Black, 4).ToList();
        private List<Color> _darkAccentColorsOctTree = Enumerable.Repeat(Colors.Black, 4).ToList();

        private List<Color> _lightAccentColorsAuto = Enumerable.Repeat(Colors.Black, 4).ToList();
        private List<Color> _darkAccentColorsAuto = Enumerable.Repeat(Colors.Black, 4).ToList();

        private BitmapDecoder? _albumArtBitmapDecoder = null;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapImage? AlbumArtBitmapImage { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial IRandomAccessStream? AlbumArtBitmapStream { get; set; }

        private void UpdateAlbumArt(bool ignoreCache = false)
        {
            _ = _albumArtRefreshRunner.RunAsync(async (token) =>
            {
                await RefreshArtAlbumAsync(ignoreCache, token);
            });
        }

        private async Task RefreshArtAlbumAsync(bool ignoreCache, CancellationToken token)
        {
            _logger.LogInformation("RefreshArtAlbum");

            IBuffer? buffer = null;
            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                buffer = await Task.Run(async () => await _albumArtSearchService.SearchAsync(CurrentSongInfo, _SMTCAlbumArtBuffer, ignoreCache, token), token);
                if (token.IsCancellationRequested) return;
            }

            if (buffer == null)
            {
                using var placeHolderStream = await ImageHelper.GetAlbumArtPlaceholderAsync();
                var tempBuffer = new Windows.Storage.Streams.Buffer((uint)placeHolderStream.Size);
                await placeHolderStream.ReadAsync(tempBuffer, (uint)placeHolderStream.Size, InputStreamOptions.None);
                if (token.IsCancellationRequested) return;

                buffer = tempBuffer;
            }

            _albumArtBitmapDecoder = await ImageHelper.GetBitmapDecoder(buffer);
            if (token.IsCancellationRequested) return;

            _lightAccentColorsMedianCut =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.MedianCut, false))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            _darkAccentColorsMedianCut =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.MedianCut, true))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();

            _lightAccentColorsOctTree =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.OctTree, false))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            _darkAccentColorsOctTree =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.OctTree, true))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();

            _lightAccentColorsAuto =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.Auto, false))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            _darkAccentColorsAuto =
                (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, PaletteGeneratorType.Auto, true))
                .Palette.Select(Helper.ColorHelper.FromVector3).ToList();

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(ImageHelper.ToIRandomAccessStream(buffer));
            if (token.IsCancellationRequested) return;

            AlbumArtBitmapImage = bitmapImage;
            AlbumArtBitmapStream = ImageHelper.ToIRandomAccessStream(buffer);
        }

        public AlbumArtThemeColors CalculateAlbumArtThemeColors(LyricsWindowStatus lyricsWindowStatus, Color backdropAccentColor)
        {
            var lightAccentColors = lyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType switch
            {
                PaletteGeneratorType.MedianCut => _lightAccentColorsMedianCut,
                PaletteGeneratorType.OctTree => _lightAccentColorsOctTree,
                PaletteGeneratorType.Auto => _lightAccentColorsAuto,
                _ => _lightAccentColorsMedianCut,
            };
            var darkAccentColors = lyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType switch
            {
                PaletteGeneratorType.MedianCut => _darkAccentColorsMedianCut,
                PaletteGeneratorType.OctTree => _darkAccentColorsOctTree,
                PaletteGeneratorType.Auto => _darkAccentColorsAuto,
                _ => _darkAccentColorsMedianCut,
            };

            var result = new AlbumArtThemeColors();
            result.EnvColor = backdropAccentColor;

            ElementTheme themeTypeSent;
            if (lyricsWindowStatus.IsAdaptToEnvironment)
            {
                themeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(result.EnvColor);
            }
            else
            {
                themeTypeSent = lyricsWindowStatus.LyricsBackgroundSettings.LyricsBackgroundTheme;
            }

            bool isLight = themeTypeSent switch
            {
                ElementTheme.Default => Application.Current.RequestedTheme == ApplicationTheme.Light,
                ElementTheme.Light => true,
                ElementTheme.Dark => false,
                _ => false
            };

            Color adaptiveGrayedFontColor;
            Color grayedEnvironmentalColor;
            Color? adaptiveColoredFontColor;

            Color darkColor = Colors.Black;
            Color lightColor = Colors.White;

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
                adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(result.EnvColor);
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
            result.BgFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor,
                _ => adaptiveGrayedFontColor,
            };

            // 前景字色
            result.FgFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsFgFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomFgFontColor,
                _ => adaptiveGrayedFontColor,
            };

            // 描边颜色
            result.StrokeFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsStrokeFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => grayedEnvironmentalColor.WithBrightness(0.7),
                LyricsFontColorType.AdaptiveColored => result.EnvColor.WithBrightness(0.7),
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomStrokeFontColor,
                _ => Colors.Transparent,
            };
            return result;
        }

        public List<Color> GetAlbumArtAccentColors(PaletteGeneratorType paletteGeneratorType, bool isDark)
        {
            var lightAccentColors = paletteGeneratorType switch
            {
                PaletteGeneratorType.MedianCut => _lightAccentColorsMedianCut,
                PaletteGeneratorType.OctTree => _lightAccentColorsOctTree,
                PaletteGeneratorType.Auto => _lightAccentColorsAuto,
                _ => _lightAccentColorsMedianCut,
            };
            var darkAccentColors = paletteGeneratorType switch
            {
                PaletteGeneratorType.MedianCut => _darkAccentColorsMedianCut,
                PaletteGeneratorType.OctTree => _darkAccentColorsOctTree,
                PaletteGeneratorType.Auto => _darkAccentColorsAuto,
                _ => _darkAccentColorsMedianCut,
            };
            return isDark ? darkAccentColors : lightAccentColors;
        }

    }
}
