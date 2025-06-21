using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Lyricify.Lyrics.Models;

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
                    ParseLyricsFromLrc(raw, durationMs);
                    break;
                case LyricsFormat.DecryptedQrc:
                    ParseLyricsFromQrc(raw, durationMs);
                    break;
                default:
                    break;
            }

            if (_lyricsLines != null && _lyricsLines.Count > 0 && _lyricsLines[0].StartMs > 0)
            {
                _lyricsLines.Insert(
                    0,
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = _lyricsLines[0].StartMs,
                        Texts = [""],
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
        private void ParseLyricsFromLrc(string raw, int durationMs)
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
                        endTimestampMs = durationMs;
                    }

                    lyricsLine ??= new LyricsLine { StartMs = startTimestampMs };

                    lyricsLine.Texts.Add(lyricsPhrase.Text);

                    if (endTimestampMs == startTimestampMs)
                    {
                        continue;
                    }
                    else
                    {
                        lyricsLine.EndMs = endTimestampMs;
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
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var lineRead = lines[lineIndex];
                    var lineWrite = new LyricsLine
                    {
                        StartMs = lineRead.StartTime ?? 0,
                        Texts = [lineRead.Text],
                        CharTimings = [],
                    };

                    var syllables = (lineRead as SyllableLineInfo)?.Syllables;
                    if (syllables != null)
                    {
                        for (
                            int syllableIndex = 0;
                            syllableIndex < syllables.Count;
                            syllableIndex++
                        )
                        {
                            var syllable = syllables[syllableIndex];
                            var charTiming = new CharTiming { StartMs = syllable.StartTime };
                            if (syllableIndex + 1 < syllables.Count)
                            {
                                charTiming.EndMs = syllables[syllableIndex + 1].StartTime;
                            }
                            else
                            {
                                charTiming.EndMs = syllable.EndTime;
                            }
                            lineWrite.CharTimings.Add(charTiming);
                        }
                    }

                    if (lineIndex + 1 < lines.Count)
                    {
                        lineWrite.EndMs = lines[lineIndex + 1].StartTime ?? 0;
                    }
                    else
                    {
                        lineWrite.EndMs = durationMs ?? 0;
                    }

                    _lyricsLines.Add(lineWrite);
                }
            }
        }
    }
}
