using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Helper
{
    public class LyricsParser
    {
        private List<LyricsLine> _lyricsLines = [];

        public List<LyricsLine> Parse(
            string raw,
            LyricsFormat? lyricsFormat = null,
            string? title = null,
            string? artist = null,
            int durationMs = 0
        )
        {
            switch (lyricsFormat)
            {
                case LyricsFormat.Lrc:
                    ParseLyricsFromLrc(raw);
                    break;
                case LyricsFormat.DecryptedQrc:
                    ParseLyricsFromQrc(raw, durationMs);
                    break;
                default:
                    break;
            }

            if (
                title != null
                && artist != null
                && _lyricsLines != null
                && _lyricsLines.Count > 0
                && _lyricsLines[0].StartTimestampMs > 0
            )
            {
                _lyricsLines.Insert(
                    0,
                    new LyricsLine
                    {
                        StartTimestampMs = 0,
                        EndTimestampMs = _lyricsLines[0].StartTimestampMs,
                        Texts = [$"{artist} - {title}"],
                    }
                );
            }
            return _lyricsLines;
        }

        /// <summary>
        /// Try to parse lyrics from the track, optionally override the raw lyrics string.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="raw"></param>
        private void ParseLyricsFromLrc(string raw)
        {
            Track track = new() { Lyrics = new() };
            track.Lyrics.ParseLRC(raw);
            var lines = track.Lyrics.SynchronizedLyrics;

            if (lines != null && lines.Count > 0)
            {
                _lyricsLines = [];
                LyricsLine? lyricsLine = null;
                for (int i = 0; i < lines.Count; i++)
                {
                    var lyricsPhrase = lines[i];
                    int startTimestampMs = lyricsPhrase.TimestampMs;
                    int endTimestampMs;

                    if (i + 1 < lines.Count)
                    {
                        endTimestampMs = lines[i + 1].TimestampMs;
                    }
                    else
                    {
                        endTimestampMs = (int)track.DurationMs;
                    }

                    lyricsLine ??= new LyricsLine { StartTimestampMs = startTimestampMs };

                    lyricsLine.Texts.Add(lyricsPhrase.Text);

                    if (endTimestampMs == startTimestampMs)
                    {
                        continue;
                    }
                    else
                    {
                        lyricsLine.EndTimestampMs = endTimestampMs;
                        _lyricsLines.Add(lyricsLine);
                        lyricsLine = null;
                    }
                }
            }
        }

        private void ParseLyricsFromQrc(string raw, int? durationMs)
        {
            var lines = Lyricify
                .Lyrics.Parsers.QrcParser.Parse(raw)
                .Lines?.Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .ToList();

            if (lines != null && lines.Count > 0)
            {
                _lyricsLines = [];
                for (int i = 0; i < lines.Count; i++)
                {
                    var lineRead = lines[i];
                    var lineWrite = new LyricsLine
                    {
                        StartTimestampMs = lineRead.StartTime ?? 0,
                        Texts = [lineRead.Text],
                    };
                    if (i + 1 < lines.Count)
                    {
                        lineWrite.EndTimestampMs = lines[i + 1].StartTime ?? 0;
                    }
                    else
                    {
                        lineWrite.EndTimestampMs = (int)(durationMs ?? 0);
                    }
                    _lyricsLines.Add(lineWrite);
                }
            }
        }
    }
}
