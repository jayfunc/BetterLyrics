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

            byte[]? bytes = await _albumArtSearchService.SearchAsync(
                SongInfo?.SourceAppUserModelId ?? "",
                _cachedSongInfo.Title,
                _cachedSongInfo.Artist,
                _cachedSongInfo?.Album ?? string.Empty,
                _SMTCAlbumArtBytes
            );
            token.ThrowIfCancellationRequested();

            if (bytes == null)
            {
                bytes = await ImageHelper.CreateTextPlaceholderBytesAsync(500, 500);
                token.ThrowIfCancellationRequested();
            }

            bytes = ImageHelper.MakeSquareWithThemeColor(bytes);

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            token.ThrowIfCancellationRequested();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();

            var albumArtSwBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba16, BitmapAlphaMode.Premultiplied);
            token.ThrowIfCancellationRequested();

            var albumArtLightAccentColor = ImageHelper.GetAccentColorsFromByte(bytes, 1, false).FirstOrDefault();
            var albumArtDarkAccentColor = ImageHelper.GetAccentColorsFromByte(bytes, 1, true).FirstOrDefault();

            AlbumArtChanged?.Invoke(this, new AlbumArtChangedEventArgs(null, albumArtSwBitmap, albumArtLightAccentColor, albumArtDarkAccentColor));
        }
    }
}
