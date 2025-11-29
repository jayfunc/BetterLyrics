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

        private Color _envColor = Colors.Transparent;
        private List<Color> _lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
        private List<Color> _darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapImage? AlbumArtBitmapImage { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtThemeColors AlbumArtThemeColors { get; set; } = new();

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

            BitmapDecoder? decoder = null;

            if (buffer == null)
            {
                using var placeHolderStream = await ImageHelper.GetAlbumArtPlaceholderAsync();
                var tempBuffer = new Windows.Storage.Streams.Buffer((uint)placeHolderStream.Size);
                await placeHolderStream.ReadAsync(tempBuffer, (uint)placeHolderStream.Size, InputStreamOptions.None);
                if (token.IsCancellationRequested) return;

                buffer = tempBuffer;
            }

            decoder = await ImageHelper.MakeSquareWithThemeColor(buffer, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType);
            if (token.IsCancellationRequested) return;

            var lightPalette = await ImageHelper.GetAccentColorsAsync(decoder, 4, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, false);
            var darkPalette = await ImageHelper.GetAccentColorsAsync(decoder, 4, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, true);
            if (token.IsCancellationRequested) return;

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(ImageHelper.ToIRandomAccessStream(buffer));
            if (token.IsCancellationRequested) return;

            _lightAccentColors = lightPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            _darkAccentColors = darkPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();

            UpdateAlbumArtThemeColors();

            AlbumArtBitmapImage = bitmapImage;
        }

        private void UpdateAlbumArtThemeColors()
        {
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            var result = new AlbumArtThemeColors();
            result.EnvColor = _envColor;

            ElementTheme themeTypeSent;
            if (status.IsAdaptToEnvironment)
            {
                themeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(result.EnvColor);
            }
            else
            {
                themeTypeSent = status.LyricsBackgroundSettings.LyricsBackgroundTheme;
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

                result.AccentColor1 = _lightAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _lightAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _lightAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _lightAccentColors.ElementAtOrDefault(3);
            }
            else
            {
                adaptiveGrayedFontColor = lightColor;
                // brightness = 0.3f;
                grayedEnvironmentalColor = darkColor;

                result.AccentColor1 = _darkAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _darkAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _darkAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _darkAccentColors.ElementAtOrDefault(3);
            }

            if (status.IsAdaptToEnvironment)
            {
                adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(result.EnvColor);
            }
            else
            {
                if (isLight)
                    adaptiveColoredFontColor = _darkAccentColors.ElementAtOrDefault(0);
                else
                    adaptiveColoredFontColor = _lightAccentColors.ElementAtOrDefault(0);
            }

            result.ThemeType = themeTypeSent;

            // 背景字色
            switch (status.LyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.BgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.BgFontColor = status.LyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 前景字色
            switch (status.LyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.FgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.FgFontColor = status.LyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 描边颜色
            switch (status.LyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.StrokeFontColor = grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.StrokeFontColor = result.EnvColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    result.StrokeFontColor = status.LyricsStyleSettings.LyricsCustomStrokeFontColor;
                    break;
                default:
                    result.StrokeFontColor = Colors.Transparent;
                    break;
            }

            AlbumArtThemeColors = result;
        }

    }
}
