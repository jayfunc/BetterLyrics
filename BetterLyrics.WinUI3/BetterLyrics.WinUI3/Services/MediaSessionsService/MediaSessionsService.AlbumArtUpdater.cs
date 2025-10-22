using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
            if (_cachedSongInfo == null)
            {
                _logger.LogWarning("Cached song info is null, cannot update album art.");
                return;
            }

            byte[]? bytes = await Task.Run(async () => await _albumArtSearchService.SearchAsync(
                SongInfo?.PlayerId ?? "",
                _cachedSongInfo.Title,
                _cachedSongInfo.Artist,
                _cachedSongInfo?.Album ?? string.Empty,
                _SMTCAlbumArtBytes,
                token
            ), token);
            if (token.IsCancellationRequested) return;

            if (bytes == null)
            {
                bytes = await ImageHelper.CreateTextPlaceholderBytesAsync(500, 500);
                token.ThrowIfCancellationRequested();
            }

            bytes = await ImageHelper.MakeSquareWithThemeColor(bytes);

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            token.ThrowIfCancellationRequested();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();

            var albumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            albumArtSwBitmap = SoftwareBitmap.Copy(albumArtSwBitmap);
            token.ThrowIfCancellationRequested();

            var albumArtLightAccentColors = await ImageHelper.GetAccentColorsFromByteAsync(bytes, 4, false);
            var lightColorBytes = albumArtLightAccentColors.Palette.Select(t => Windows.UI.Color.FromArgb(255, (byte)t.X, (byte)t.Y, (byte)t.Z)).ToList();
            var albumArtDarkAccentColors = await ImageHelper.GetAccentColorsFromByteAsync(bytes, 4, true);
            var darkColorBytes = albumArtDarkAccentColors.Palette.Select(t => Windows.UI.Color.FromArgb(255, (byte)t.X, (byte)t.Y, (byte)t.Z)).ToList();
            AlbumArtChanged?.Invoke(this, new AlbumArtChangedEventArgs(null, albumArtSwBitmap, lightColorBytes, darkColorBytes));
        }
    }
}
