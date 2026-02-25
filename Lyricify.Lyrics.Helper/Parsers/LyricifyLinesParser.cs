using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Parsers
{
    public static class LyricifyLinesParser
    {
        public static LyricsData Parse(string lyrics)
        {
            var lyricsLines = lyrics.Replace("[type:LyricifyLines]", "").Trim().Split('\n').ToList();
            var data = new LyricsData
            {
                TrackMetadata = new TrackMetadata(),
                File = new()
                {
                    SyncTypes = SyncTypes.LineSynced,
                    Type = LyricsTypes.LyricifyLines,
                    AdditionalInfo = new GeneralAdditionalInfo()
                    {
                        Attributes = new(),
                    }
                }
            };

            // 处理 Attributes
            var offset = AttributesHelper.ParseGeneralAttributesToLyricsData(data, lyricsLines);

            var lines = ParseLyrics(lyricsLines, offset);
            data.Lines = lines.Cast<ILineInfo>().ToList();

            return data;
        }

        public static List<LineInfo> ParseLyrics(List<string> lines, int? offset = null)
        {
            offset ??= 0;
            var lyricsArray = new List<LineInfo>();
            foreach (var line in lines)
            {
                if (!line.StartsWith('[') || !line.Contains(',') || !line.Contains(']')) continue;
                try
                {
                    int begin = int.Parse(line.Between("[", ","));
                    int end = int.Parse(line.Between(",", "]"));
                    string text = line[(line.IndexOf(']') + 1)..].Trim();
                    lyricsArray.Add(new()
                    {
                        Text = text,
                        StartTime = begin - offset,
                        EndTime = end - offset,
                    });
                }
                catch { }
            }
            return lyricsArray;
        }
    }
}
