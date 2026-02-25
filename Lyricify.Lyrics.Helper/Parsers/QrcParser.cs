using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Parsers
{
    public static class QrcParser
    {
        public static LyricsData Parse(string lyrics)
        {
            var lyricsLines = lyrics.Trim().Split('\n').ToList();
            var data = new LyricsData
            {
                TrackMetadata = new TrackMetadata(),
                File = new()
                {
                    Type = LyricsTypes.Qrc,
                    SyncTypes = SyncTypes.SyllableSynced,
                    AdditionalInfo = new GeneralAdditionalInfo()
                    {
                        Attributes = new(),
                    }
                }
            };

            // 处理 Attributes
            var offset = AttributesHelper.ParseGeneralAttributesToLyricsData(data, lyricsLines);

            // 处理歌词行
            var lines = ParseLyrics(lyricsLines, offset);

            data.Lines = lines;
            return data;
        }

        /// <summary>
        /// 解析 QRC 歌词
        /// </summary>
        public static List<ILineInfo> ParseLyrics(List<string> lines, int? offset = null)
        {
            var list = new List<SyllableLineInfo>();

            foreach (var line in lines)
            {
                // 处理歌词行
                var item = ParseLyricsLine(line);
                if (item != null)
                {
                    list.Add(item);
                }
            }

            var returnList = list.Cast<ILineInfo>().ToList();
            if (offset.HasValue && offset.Value != 0)
            {
                OffsetHelper.AddOffset(returnList, offset.Value);
            }

            return returnList;
        }

        /// <summary>
        /// 解析 QRC 歌词行
        /// </summary>
        public static SyllableLineInfo? ParseLyricsLine(string line)
        {
            if (line.IndexOf(']') != -1)
            {
                line = line[(line.IndexOf("]") + 1)..];
            }

            List<SyllableInfo> lyricItems = new();
            MatchCollection matches = Regex.Matches(line, @"(.*?)\((\d+),(\d+)\)");

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Groups.Count == 4)
                {
                    string text = match.Groups[1].Value;
                    int startTime = int.Parse(match.Groups[2].Value);
                    int duration = int.Parse(match.Groups[3].Value);

                    int endTime = startTime + duration;

                    lyricItems.Add(new() { Text = text, StartTime = startTime, EndTime = endTime });
                }
            }

            return new(lyricItems);
        }
    }
}
