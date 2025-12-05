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

    }
}
