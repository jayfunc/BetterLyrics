using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
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

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public partial class MediaSessionsService : IMediaSessionsService
    {
        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapDecoder? AlbumArtBitmapDecoder { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapImage? AlbumArtBitmapImage { get; set; }

        private void UpdateAlbumArt()
        {
            _ = _albumArtRefreshRunner.RunAsync(RefreshArtAlbum);
        }

        private async Task RefreshArtAlbum(CancellationToken token)
        {
            _logger.LogInformation("RefreshArtAlbum");

            if (CurrentSongInfo == null)
            {
                _logger.LogWarning("CurrentSongInfo == null");
                return;
            }

            IBuffer? buffer = await Task.Run(async () => await _albumArtSearchService.SearchAsync(CurrentSongInfo, _SMTCAlbumArtBuffer, token), token);
            if (token.IsCancellationRequested) return;

            if (buffer == null)
            {
                using var placeHolderStream = await ImageHelper.GetAlbumArtPlaceholderAsync();
                var tempBuffer = new Windows.Storage.Streams.Buffer((uint)placeHolderStream.Size);
                await placeHolderStream.ReadAsync(tempBuffer, (uint)placeHolderStream.Size, InputStreamOptions.None);
                if (token.IsCancellationRequested) return;

                buffer = tempBuffer;
            }

            AlbumArtBitmapDecoder = await ImageHelper.GetBitmapDecoder(buffer);
            if (token.IsCancellationRequested) return;

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(ImageHelper.ToIRandomAccessStream(buffer));
            if (token.IsCancellationRequested) return;

            AlbumArtBitmapImage = bitmapImage;
        }

        public async Task<AlbumArtThemeColors> CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus lyricsWindowStatus, Color backdropAccentColor)
        {
            List<Color> lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
            List<Color> darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

            if (AlbumArtBitmapDecoder is BitmapDecoder decoder)
            {
                var lightPalette = await ImageHelper.GetAccentColorsAsync(AlbumArtBitmapDecoder, 4, lyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, false);
                var darkPalette = await ImageHelper.GetAccentColorsAsync(AlbumArtBitmapDecoder, 4, lyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, true);
                lightAccentColors = lightPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                darkAccentColors = darkPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            }

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
            switch (lyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.BgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.BgFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 前景字色
            switch (lyricsWindowStatus.LyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.FgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.FgFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 描边颜色
            switch (lyricsWindowStatus.LyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.StrokeFontColor = grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.StrokeFontColor = result.EnvColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    result.StrokeFontColor = lyricsWindowStatus.LyricsStyleSettings.LyricsCustomStrokeFontColor;
                    break;
                default:
                    result.StrokeFontColor = Colors.Transparent;
                    break;
            }

            return result;
        }

    }
}
