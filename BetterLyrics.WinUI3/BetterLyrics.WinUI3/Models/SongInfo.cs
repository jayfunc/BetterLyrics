using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATL;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Windows.UI;
using static ATL.LyricsInfo;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject
    {
        [ObservableProperty]
        public partial string? Title { get; set; }

        [ObservableProperty]
        public partial string? Artist { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string>? FilesFound { get; set; }

        [ObservableProperty]
        public partial bool IsLyricsExisted { get; set; } = false;

        [ObservableProperty]
        public partial string? SourceAppUserModelId { get; set; } = null;

        [ObservableProperty]
        public partial List<LyricsLine>? LyricsLines { get; set; } = null;
        public byte[]? AlbumArt { get; set; } = null;

        [ObservableProperty]
        public partial List<Color>? CoverImageDominantColors { get; set; } = null;

        public SongInfo() { }

        /// <summary>
        /// Try to parse lyrics from the track, optionally override the raw lyrics string.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="overrideRaw"></param>
        public void ParseLyrics(Track track, string? overrideRaw = null)
        {
            List<LyricsLine>? result = null;

            if (overrideRaw != null)
                track.Lyrics.ParseLRC(overrideRaw);

            var lyricsPhrases = track.Lyrics.SynchronizedLyrics;

            if (lyricsPhrases?.Count > 0)
            {
                if (lyricsPhrases[0].TimestampMs > 0)
                {
                    var placeholder = new LyricsPhrase(0, $"{track.Artist} - {track.Title}");
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
                    result ??= [];
                    result.Add(lyricsLine);
                    lyricsLine = null;
                }
            }

            if (result != null && result.Count == 0)
            {
                result = null;
            }

            LyricsLines = result;
            IsLyricsExisted = result != null;
        }
    }
}
