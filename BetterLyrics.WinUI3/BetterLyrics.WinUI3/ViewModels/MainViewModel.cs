using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using Ude;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;
using static ATL.LyricsInfo;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isAnyMusicSessionExisted = false;

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        [ObservableProperty]
        private ObservableCollection<Color> _coverImageDominantColors;

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

        private readonly ColorThief _colorThief = new();

        private readonly SettingsService _settingsService;
        private readonly DatabaseService _databaseService;

        private readonly int _accentColorCount = 3;

        public MainViewModel(SettingsService settingsService, DatabaseService databaseService)
        {
            _settingsService = settingsService;
            _databaseService = databaseService;
            CoverImageDominantColors =
            [
                .. Enumerable.Repeat(Colors.Transparent, _accentColorCount),
            ];
        }

        public List<LyricsLine> GetLyrics(Track? track)
        {
            List<LyricsLine> result = [];

            var lyricsPhrases = track?.Lyrics?.SynchronizedLyrics;

            if (lyricsPhrases?.Count > 0)
            {
                if (lyricsPhrases[0].TimestampMs > 0)
                {
                    var placeholder = new LyricsPhrase(0, " ");
                    lyricsPhrases.Insert(0, placeholder);
                    lyricsPhrases.Insert(0, placeholder);
                }
            }

            LyricsLine? lyricsLine = null;

            for (int i = 0; i < lyricsPhrases?.Count; i++)
            {
                var lyricsPhrase = lyricsPhrases[i];
                int startTimestampMs = lyricsPhrase.TimestampMs;
                int endTimestampMs;

                if (i + 1 < lyricsPhrases.Count)
                {
                    endTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                }
                else
                {
                    endTimestampMs = (int)track.DurationMs;
                }

                lyricsLine ??= new LyricsLine { StartPlayingTimestampMs = startTimestampMs };

                lyricsLine.Texts.Add(lyricsPhrase.Text);

                if (endTimestampMs == startTimestampMs)
                {
                    continue;
                }
                else
                {
                    lyricsLine.EndPlayingTimestampMs = endTimestampMs;
                    result.Add(lyricsLine);
                    lyricsLine = null;
                }
            }

            LyricsExisted = result.Count != 0;
            if (!LyricsExisted)
            {
                ShowLyricsOnly = false;
            }

            return result;
        }

        public async Task<(List<LyricsLine>, SoftwareBitmap?)> SetSongInfoAsync(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            SoftwareBitmap? coverSoftwareBitmap = null;
            uint coverImagePixelWidth = 0;
            uint coverImagePixelHeight = 0;

            Title = mediaProps?.Title;
            Artist = mediaProps?.Artist;

            IRandomAccessStream? stream = null;

            var track = _databaseService.GetMusicMetadata(mediaProps);

            if (mediaProps?.Thumbnail is IRandomAccessStreamReference reference)
            {
                stream = await reference.OpenReadAsync();
            }
            else
            {
                if (track?.EmbeddedPictures.Count > 0)
                {
                    var bytes = track.EmbeddedPictures[0].PictureData;
                    if (bytes != null)
                    {
                        stream = await Helper.ImageHelper.GetStreamFromBytesAsync(bytes);
                    }
                }
            }

            // Set cover image and dominant colors
            if (stream == null)
            {
                CoverImage = null;
                CoverImageDominantColors =
                [
                    .. Enumerable.Repeat(Colors.Transparent, _accentColorCount),
                ];
                _settingsService.LyricsFontSelectedAccentColorIndex =
                    _settingsService.LyricsFontSelectedAccentColorIndex;
            }
            else
            {
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

                CoverImageDominantColors =
                [
                    .. (await _colorThief.GetPalette(decoder, _accentColorCount)).Select(color =>
                        Color.FromArgb(color.Color.A, color.Color.R, color.Color.G, color.Color.B)
                    ),
                ];
                _settingsService.LyricsFontSelectedAccentColorIndex =
                    _settingsService.LyricsFontSelectedAccentColorIndex;

                stream.Dispose();
            }

            return (GetLyrics(track), coverSoftwareBitmap);
        }
    }
}
