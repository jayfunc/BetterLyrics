using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public partial class MediaSessionsService : IMediaSessionsService
    {
        private readonly LatestOnlyTaskRunner _albumArtRefreshRunner = new();

        public event EventHandler<AlbumArtChangedEventArgs>? AlbumArtChanged;

        private void UpdateAlbumArt()
        {
            _albumArtRefreshRunner.RunAsync(RefreshArtAlbum);
        }

        private async Task RefreshArtAlbum(CancellationToken token)
        {
            if (CurrentSongInfo == null)
            {
                _logger.LogWarning("Cached song info is null, cannot update album art.");
                return;
            }

            IBuffer? buffer = await Task.Run(async () => await _albumArtSearchService.SearchAsync(
                CurrentSongInfo?.PlayerId ?? "",
                CurrentSongInfo.Title,
                CurrentSongInfo.Artist,
                CurrentSongInfo.Album,
                _SMTCAlbumArtBuffer,
                token
            ), token);
            if (token.IsCancellationRequested) return;
            BitmapDecoder? decoder = null;

            if (buffer == null)
            {
                using var placeHolderStream = await ImageHelper.GetAlbumArtPlaceholderAsync();
                var tempBuffer = new Windows.Storage.Streams.Buffer((uint)placeHolderStream.Size);
                await placeHolderStream.ReadAsync(tempBuffer, (uint)placeHolderStream.Size, InputStreamOptions.None);
                buffer = tempBuffer;
                token.ThrowIfCancellationRequested();
            }
            decoder = await ImageHelper.MakeSquareWithThemeColor(buffer, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType);
            token.ThrowIfCancellationRequested();

            var albumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            albumArtSwBitmap.DpiX = 96;
            albumArtSwBitmap.DpiY = 96;
            token.ThrowIfCancellationRequested();

            var albumArtLightAccentColors = await ImageHelper.GetAccentColorsAsync(decoder, 4, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, false);
            var lightColorBytes = albumArtLightAccentColors.Palette.Select(t => Windows.UI.Color.FromArgb(255, (byte)t.X, (byte)t.Y, (byte)t.Z)).ToList();
            var albumArtDarkAccentColors = await ImageHelper.GetAccentColorsAsync(decoder, 4, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PaletteGeneratorType, true);
            var darkColorBytes = albumArtDarkAccentColors.Palette.Select(t => Windows.UI.Color.FromArgb(255, (byte)t.X, (byte)t.Y, (byte)t.Z)).ToList();
            AlbumArtChanged?.Invoke(this, new AlbumArtChangedEventArgs(null, albumArtSwBitmap, lightColorBytes, darkColorBytes));
        }
    }
}
