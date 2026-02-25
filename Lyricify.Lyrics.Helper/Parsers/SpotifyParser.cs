using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Parsers.Models.Spotify;
using Newtonsoft.Json;

namespace Lyricify.Lyrics.Parsers
{
    public static class SpotifyParser
    {
        public static LyricsData? Parse(string rawJson)
        {
            var colorLyrics = JsonConvert.DeserializeObject<SpotifyColorLyrics>(rawJson);
            if (colorLyrics != null && colorLyrics.Lyrics != null)
            {
                var lyrics = ParseLyrics(colorLyrics.Lyrics);
                var lyricsData = new LyricsData
                {
                    File = new(),
                    Lines = lyrics,
                };
                lyricsData.File.Type = LyricsTypes.Spotify;
                lyricsData.File.SyncTypes = colorLyrics.Lyrics.SyncType switch
                {
                    "UNSYNCED" => SyncTypes.Unsynced,
                    "LINE_SYNCED" => SyncTypes.LineSynced,
                    "SYLLABLE_SYNCED" => SyncTypes.SyllableSynced,
                    _ => SyncTypes.Unknown,
                };
                lyricsData.File.AdditionalInfo = new SpotifyAdditionalInfo(colorLyrics.Lyrics);
                return lyricsData;
            }
            return null;
        }

        public static List<ILineInfo> ParseLyrics(SpotifyLyrics lyrics)
        {
            if (lyrics.SyncType == "UNSYNCED")
            {
                return ParseUnsyncedLyrics(lyrics.Lines).Cast<ILineInfo>().ToList();
            }
            else
            {
                return ParseSyncedLyrics(lyrics.Lines);
            }
        }

        public static List<LineInfo> ParseUnsyncedLyrics(List<SpotifyLyricsLine> lyrics)
        {
            var list = new List<LineInfo>();
            foreach (var line in lyrics)
            {
                list.Add(new(line.Words));
            }
            return list;
        }

        public static List<ILineInfo> ParseSyncedLyrics(List<SpotifyLyricsLine> lyrics)
        {
            var list = new List<ILineInfo>();
            foreach (var line in lyrics)
            {
                if (line.Syllables is { Count: > 0 })
                {
                    var syllables = new List<SyllableInfo>();
                    int i = 0;
                    foreach (var syllable in line.Syllables)
                    {
                        syllables.Add(new(line.Words[i..(i + syllable.CharsCount)], syllable.StartTime, syllable.EndTime));
                        i += syllable.CharsCount;
                    }
                    list.Add(new SyllableLineInfo(syllables.Cast<ISyllableInfo>().ToList()));
                }
                else
                {
                    if (line.EndTime != 0)
                    {
                        list.Add(new LineInfo(line.Words, line.StartTime, line.EndTime));
                    }
                    else
                    {
                        list.Add(new LineInfo(line.Words, line.StartTime));
                    }
                }
            }
            return list;
        }
    }
}
