using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Lyrics;
using Lyricify.Lyrics.Models;
using LyricsData = BetterLyrics.Core.Models.Lyrics.LyricsData;

namespace BetterLyrics.Core.Helpers.Lyrics.ContentParser;

public partial class LyricsContentParser
{
    private void ParseQrcKrc(List<ILineInfo>? lines, LyricsProvider? provider)
    {
        lines = lines?.Where(x => x.Text != string.Empty).ToList();
        List<LyricsLine> lyricsLines = [];

        if (lines != null && lines.Count > 0)
        {
            lyricsLines = [];
            for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var lineRead = lines[lineIndex];
                var nextLineRead = lines.ElementAtOrDefault(lineIndex + 1);
                var lineWrite = new LyricsLine
                {
                    StartMs = lineRead.StartTime ?? 0,
                    PrimaryText = lineRead.Text,
                    IsPrimaryHasRealSyllableInfo = true
                };

                var syllables = (lineRead as SyllableLineInfo)?.Syllables;
                if (syllables != null)
                {
                    var startIndex = 0;
                    for (
                        var syllableIndex = 0;
                        syllableIndex < syllables.Count;
                        syllableIndex++
                    )
                    {
                        var syllable = syllables[syllableIndex];
                        var charTiming = new BaseLyrics
                        {
                            StartMs = syllable.StartTime,
                            EndMs = syllable.EndTime,
                            Text = syllable.Text,
                            StartIndex = startIndex
                        };
                        lineWrite.PrimarySyllables.Add(charTiming);
                        startIndex += syllable.Text.Length;
                    }
                }

                lyricsLines.Add(lineWrite);
            }
        }

        var data = new LyricsData(lyricsLines) { Provider = provider };
        if (LyricsDataArr.Count > 0)
        {
            data.TrackType = LanguageHelper.IsPhoneticTag(data.LanguageTag)
                ? LyricsTrackType.Transliteration
                : LyricsTrackType.Translation;
        }
        AddLyricsData(data);
    }
}