using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Runtime.InteropServices.WindowsRuntime;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : IGSMTCService
    {
        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();

        private BitmapDecoder? _albumArtBitmapDecoder = null;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapImage? AlbumArtBitmapImage { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial byte[]? AlbumArtBytes { get; set; }

        private void UpdateAlbumArt(bool ignoreCache = false)
        {
            _ = _albumArtRefreshRunner.RunAsync(async (token) =>
            {
                await RefreshArtAlbumAsync(ignoreCache, token);
            });
        }

        private async Task RefreshArtAlbumAsync(bool ignoreCache, CancellationToken token)
        {
            IBuffer? buffer = null;
            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                buffer = await Task.Run(async () => await _albumArtSearchService.SearchAsync(CurrentSongInfo, _SMTCAlbumArtBuffer, ignoreCache, token), token);
            }

            if (buffer == null)
            {
                using var placeHolderStream = await ImageHelper.GetAlbumArtPlaceholderAsync();
                token.ThrowIfCancellationRequested();

                var tempBuffer = new Windows.Storage.Streams.Buffer((uint)placeHolderStream.Size);

                await placeHolderStream.ReadAsync(tempBuffer, (uint)placeHolderStream.Size, InputStreamOptions.None);
                token.ThrowIfCancellationRequested();

                buffer = tempBuffer;
            }

            _albumArtBitmapDecoder = await ImageHelper.GetBitmapDecoderAsync(buffer);
            token.ThrowIfCancellationRequested();

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(ImageHelper.ToIRandomAccessStream(buffer));
            token.ThrowIfCancellationRequested();

            AlbumArtBitmapImage = bitmapImage;
            AlbumArtBytes = buffer.ToArray();
        }

        public async Task<NowPlayingPalette> CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus lyricsWindowStatus, Color backdropAccentColor, CancellationToken token = default)
        {
            var lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
            var darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

            if (_albumArtBitmapDecoder != null)
            {
                lightAccentColors =
                        (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, lyricsWindowStatus.PaletteGeneratorType, false))
                        .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                token.ThrowIfCancellationRequested();

                darkAccentColors =
                    (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, lyricsWindowStatus.PaletteGeneratorType, true))
                    .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                token.ThrowIfCancellationRequested();
            }

            var result = new NowPlayingPalette();
            result.UnderlayColor = backdropAccentColor;

            ElementTheme themeTypeSent;
            if (lyricsWindowStatus.IsAdaptToEnvironment)
            {
                themeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(result.UnderlayColor);
            }
            else
            {
                themeTypeSent = lyricsWindowStatus.WindowTheme;
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
                adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(result.UnderlayColor);
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
                _ => adaptiveGrayedFontColor,
            };

            // 频谱填充色
            result.SpectrumColor = lyricsWindowStatus.LyricsBackgroundSettings.SpectrumColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsBackgroundSettings.SpectrumCustomColor,
                _ => adaptiveGrayedFontColor,
            };

            // 前景字色
            result.PlayedCurrentLineFillColor = lyricsWindowStatus.LyricsStyleSettings.LyricsPlayedFgFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedFgFontColor,
                _ => adaptiveGrayedFontColor,
            };
            result.UnplayedCurrentLineFillColor = lyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedFgFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => adaptiveGrayedFontColor,
                LyricsFontColorType.AdaptiveColored => adaptiveColoredFontColor ?? adaptiveGrayedFontColor,
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedFgFontColor,
                _ => adaptiveGrayedFontColor,
            };

            // 描边颜色
            result.PlayedTextStrokeColor = lyricsWindowStatus.LyricsStyleSettings.LyricsPlayedStrokeFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => grayedEnvironmentalColor.WithBrightness(0.7),
                LyricsFontColorType.AdaptiveColored => result.UnderlayColor.WithBrightness(0.7),
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedStrokeFontColor,
                _ => Colors.Transparent,
            };
            result.UnplayedTextStrokeColor = lyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedStrokeFontColorType switch
            {
                LyricsFontColorType.AdaptiveGrayed => grayedEnvironmentalColor.WithBrightness(0.7),
                LyricsFontColorType.AdaptiveColored => result.UnderlayColor.WithBrightness(0.7),
                LyricsFontColorType.Custom => lyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedStrokeFontColor,
                _ => Colors.Transparent,
            };
            return result;
        }

        public async Task<List<Color>> GetAlbumArtAccentColorsAsync(PaletteGeneratorType paletteGeneratorType, bool isDark, CancellationToken token = default)
        {
            var lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
            var darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

            if (_albumArtBitmapDecoder != null)
            {
                lightAccentColors =
                        (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, paletteGeneratorType, false))
                        .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                token.ThrowIfCancellationRequested();

                darkAccentColors =
                    (await ImageHelper.GetAccentColorsAsync(_albumArtBitmapDecoder, 4, paletteGeneratorType, true))
                    .Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                token.ThrowIfCancellationRequested();
            }
            return isDark ? darkAccentColors : lightAccentColors;
        }

    }
}
