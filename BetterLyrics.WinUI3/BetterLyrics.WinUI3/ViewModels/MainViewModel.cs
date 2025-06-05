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
    public partial class MainViewModel(SettingsService settingsService) : ObservableObject
    {
        private readonly CharsetDetector _charsetDetector = new();

        [ObservableProperty]
        private bool _isAnyMusicSessionExisted = false;

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        [ObservableProperty]
        private ObservableCollection<Windows.UI.Color> _coverImageDominantColors =
        [
            Colors.Transparent,
            Colors.Transparent,
            Colors.Transparent,
        ];

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

        private readonly Helper.ColorThief _colorThief = new();

        private readonly SettingsService _settingsService = settingsService;

        public List<LyricsLine> GetLyrics(Track? track)
        {
            List<LyricsLine> result = [];

            var lyricsPhrases = track?.Lyrics.SynchronizedLyrics;

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

        public async Task<Track?> FindTrackFromDirectories(
            IEnumerable<string> directories,
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            Track? finalResult = null;

            if (mediaProps == null || mediaProps.Title == null || mediaProps.Artist == null)
                return finalResult;

            finalResult = new() { Title = mediaProps.Title, Artist = mediaProps.Artist };

            bool coverFound = false;
            bool lyricsFound = false;

            if (mediaProps.Thumbnail is IRandomAccessStreamReference streamReference)
            {
                coverFound = true;
                PictureInfo pictureInfo = PictureInfo.fromBinaryData(
                    await ImageHelper.ToByteArrayAsync(streamReference)
                );
                finalResult.EmbeddedPictures.Add(pictureInfo);
            }

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                    continue;

                foreach (
                    var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                )
                {
                    string content = File.ReadAllText(file);
                    var result = new Track(content);

                    if (
                        result.Title.Contains(mediaProps.Title)
                        && result.Artist.Contains(mediaProps.Artist)
                    )
                    {
                        if (!coverFound && result.EmbeddedPictures.Count > 0)
                        {
                            coverFound = true;
                            finalResult.EmbeddedPictures.AddRange(result.EmbeddedPictures);
                        }

                        if (!lyricsFound && result.Lyrics != null)
                        {
                            lyricsFound = true;
                            finalResult.Lyrics = result.Lyrics;
                        }
                    }

                    if (!lyricsFound && file.EndsWith(".lrc"))
                    {
                        using (FileStream fs = File.OpenRead(file))
                        {
                            _charsetDetector.Feed(fs);
                            _charsetDetector.DataEnd();
                        }

                        if (_charsetDetector.Charset != null)
                        {
                            Encoding encoding = Encoding.GetEncoding(_charsetDetector.Charset);
                            content = File.ReadAllText(file, encoding);
                        }
                        else
                        {
                            content = File.ReadAllText(file, Encoding.UTF8);
                        }

                        lyricsFound = true;
                        finalResult.Lyrics = new();
                        finalResult.Lyrics.ParseLRC(content);
                    }

                    if (coverFound && lyricsFound)
                        return finalResult;
                }
            }

            return finalResult;
        }

        public async Task<(List<LyricsLine>, SoftwareBitmap?, uint, uint)> SetSongInfoAsync(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            SoftwareBitmap? coverSoftwareBitmap = null;
            uint coverImagePixelWidth = 0;
            uint coverImagePixelHeight = 0;

            Title = mediaProps?.Title;
            Artist = mediaProps?.Artist;

            IRandomAccessStream? stream = null;

            var track = await FindTrackFromDirectories(_settingsService.MusicLibraries, mediaProps);

            if (track?.EmbeddedPictures?[0].PictureData is byte[] bytes)
                stream = await ImageHelper.GetStreamFromBytesAsync(bytes);

            // Set cover image and dominant colors
            if (stream == null)
            {
                CoverImage = null;
                CoverImageDominantColors =
                [
                    .. Enumerable.Repeat(Microsoft.UI.Colors.Transparent, 3),
                ];
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
                    .. (await _colorThief.GetPalette(decoder, 3)).Select(quantizedColor =>
                        Windows.UI.Color.FromArgb(
                            quantizedColor.Color.A,
                            quantizedColor.Color.R,
                            quantizedColor.Color.G,
                            quantizedColor.Color.B
                        )
                    ),
                ];

                stream.Dispose();
            }

            return (
                GetLyrics(track),
                coverSoftwareBitmap,
                coverImagePixelWidth,
                coverImagePixelHeight
            );
        }
    }
}
