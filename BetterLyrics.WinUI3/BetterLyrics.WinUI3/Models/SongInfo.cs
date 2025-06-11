using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.UI;
using static ATL.LyricsInfo;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject
    {
        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        [ObservableProperty]
        private ObservableCollection<string>? _filesUsed;

        [ObservableProperty]
        private bool? _isLyricsExisted;

        [ObservableProperty]
        private string? _sourceAppUserModelId = null;

        public List<LyricsLine>? LyricsLines { get; set; } = null;

        public byte[]? AlbumArt { get; set; } = null;

        public SongInfo() { }

        /// <summary>
        /// Try to parse lyrics from the track, optionally override the raw lyrics string.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="overrideRaw"></param>
        public void ParseLyrics(Track track, string? overrideRaw = null)
        {
            List<LyricsLine> result = [];

            if (overrideRaw != null)
                track.Lyrics.ParseLRC(overrideRaw);

            var lyricsPhrases = track.Lyrics.SynchronizedLyrics;

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

            LyricsLines = result;
            IsLyricsExisted = result.Count != 0;
        }
    }
}
