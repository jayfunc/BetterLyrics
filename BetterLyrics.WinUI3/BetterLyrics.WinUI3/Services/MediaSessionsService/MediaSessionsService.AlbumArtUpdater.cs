using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
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

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial BitmapImage? AlbumArtBitmapImage { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<Color> LightAccentColors { get; set; } = Enumerable.Repeat(Colors.Black, 4).ToList();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<Color> DarkAccentColors { get; set; } = Enumerable.Repeat(Colors.Black, 4).ToList();

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

            LightAccentColors = lightPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            DarkAccentColors = darkPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();

            AlbumArtBitmapImage = bitmapImage;
        }
    }
}
