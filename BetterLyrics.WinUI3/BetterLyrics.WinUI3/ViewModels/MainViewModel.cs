using ATL;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.UI;
using static ATL.LyricsInfo;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class MainViewModel : ObservableObject {

        [ObservableProperty]
        private bool _isAnyMusicSessionExisted = false;

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        public List<Color> CoverImageDominantColors { get; set; } =
            [Colors.Transparent, Colors.Transparent, Colors.Transparent];

        [ObservableProperty]
        private BitmapImage? _coverImage;

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        [ObservableProperty]
        private bool _isSmallScreenMode;

        [ObservableProperty]
        private bool _showLyricsOnly = false;

        [ObservableProperty]
        private bool _lyricsExisted = false;

        private Helper.ColorThief _colorThief = new();

        private readonly DatabaseService _databaseService;

        public MainViewModel(DatabaseService databaseService) {
            _databaseService = databaseService;
        }

        public List<LyricsLine> GetLyrics(Track? track) {
            List<LyricsLine> result = [];
            var lyricsPhrases = track?.Lyrics.SynchronizedLyrics;

            if (lyricsPhrases?.Count > 0) {
                if (lyricsPhrases[0].TimestampMs > 0) {
                    var placeholder = new LyricsPhrase(0, " ");
                    lyricsPhrases.Insert(0, placeholder);
                    lyricsPhrases.Insert(0, placeholder);
                }
            }

            LyricsLine? lyricsLine = null;

            for (int i = 0; i < lyricsPhrases?.Count; i++) {
                var lyricsPhrase = lyricsPhrases[i];
                int startTimestampMs = lyricsPhrase.TimestampMs;
                int endTimestampMs;

                if (i + 1 < lyricsPhrases.Count) {
                    endTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                } else {
                    endTimestampMs = (int)track.DurationMs;
                }

                lyricsLine ??= new LyricsLine {
                    StartPlayingTimestampMs = startTimestampMs,
                };

                lyricsLine.Texts.Add(lyricsPhrase.Text);

                if (endTimestampMs == startTimestampMs) {
                    continue;
                } else {
                    lyricsLine.EndPlayingTimestampMs = endTimestampMs;
                    result.Add(lyricsLine);
                    lyricsLine = null;
                }

            }
            LyricsExisted = result.Count != 0;

            return result;

        }

        public async Task<(List<LyricsLine>, SoftwareBitmap?, uint, uint)> SetSongInfoAsync(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps, ICanvasAnimatedControl control) {

            SoftwareBitmap? coverSoftwareBitmap = null;
            uint coverImagePixelWidth = 0;
            uint coverImagePixelHeight = 0;

            Title = mediaProps?.Title;
            Artist = mediaProps?.Artist;

            IRandomAccessStream? stream = null;

            var track = _databaseService.GetMusicMetadata(Title, Artist);

            if (mediaProps?.Thumbnail is IRandomAccessStreamReference reference) {
                stream = await reference.OpenReadAsync();
            } else {
                if (track?.EmbeddedPictures.Count > 0) {
                    var bytes = track.EmbeddedPictures[0].PictureData;
                    if (bytes != null) {
                        stream = await Helper.ImageHelper.GetStreamFromBytesAsync(bytes);
                    }
                }
            }

            // Set cover image and dominant colors
            if (stream == null) {
                CoverImage = null;
                for (int i = 0; i < 3; i++) {
                    CoverImageDominantColors[i] = Colors.Transparent;
                }
            } else {
                CoverImage = new BitmapImage();
                await CoverImage.SetSourceAsync(stream);
                stream.Seek(0);

                var decoder = await BitmapDecoder.CreateAsync(stream);
                coverImagePixelHeight = decoder.PixelHeight;
                coverImagePixelWidth = decoder.PixelWidth;

                coverSoftwareBitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied
                );

                var quantizedColors = await _colorThief.GetPalette(decoder, 3);
                for (int i = 0; i < 3; i++) {
                    Helper.QuantizedColor quantizedColor = quantizedColors[i];
                    CoverImageDominantColors[i] = Color.FromArgb(
                        quantizedColor.Color.A, quantizedColor.Color.R, quantizedColor.Color.G, quantizedColor.Color.B);
                }

                stream.Dispose();
            }

            return (GetLyrics(track), coverSoftwareBitmap, coverImagePixelWidth, coverImagePixelHeight);

        }

    }
}
