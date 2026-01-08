using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        private void ParseQrcKrc(List<Lyricify.Lyrics.Models.ILineInfo>? lines)
        {
            lines = lines?.Where(x => x.Text != string.Empty).ToList();
            List<LyricsLine> lyricsLines = [];

            if (lines != null && lines.Count > 0)
            {
                lyricsLines = [];
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var lineRead = lines[lineIndex];
                    var lineWrite = new LyricsLine
                    {
                        StartMs = lineRead.StartTime ?? 0,
                        EndMs = lineRead.EndTime ?? 0,
                        OriginalText = lineRead.Text,
                        LyricsSyllables = [],
                    };

                    var syllables = (lineRead as Lyricify.Lyrics.Models.SyllableLineInfo)?.Syllables;
                    if (syllables != null)
                    {
                        int startIndex = 0;
                        for (
                            int syllableIndex = 0;
                            syllableIndex < syllables.Count;
                            syllableIndex++
                        )
                        {
                            var syllable = syllables[syllableIndex];
                            var charTiming = new LyricsSyllable
                            {
                                StartMs = syllable.StartTime,
                                EndMs = syllable.EndTime,
                                Text = syllable.Text,
                                StartIndex = startIndex,
                            };
                            lineWrite.LyricsSyllables.Add(charTiming);
                            startIndex += syllable.Text.Length;
                        }
                    }

                    lyricsLines.Add(lineWrite);
                }
            }

            _lyricsDataArr.Add(new LyricsData(lyricsLines));
        }

    }
}
