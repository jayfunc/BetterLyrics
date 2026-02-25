using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;
using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Parsers
{
    public static class LyricifySyllableParser
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
        /// 解析 Lyricify Syllable 歌词
        /// </summary>
        public static List<ILineInfo> ParseLyrics(List<string> lines, int? offset = null)
        {
            var list = new List<SyllableLineInfoWithSubLineState>();

            foreach (var line in lines)
            {
                // 处理歌词行
                var item = ParseLyricsLine(line);
                if (item != null)
                {
                    list.Add(item);
                }
            }

            // 将背景人声歌词放入主歌词
            var newList = SetBackgroundVocalsInfo(list);

            // 应用 Offset
            if (offset.HasValue && offset.Value != 0)
            {
                OffsetHelper.AddOffset(newList, offset.Value);
            }

            return newList;
        }

        /// <summary>
        /// 获取将背景人声歌词放入主歌词后的新列表 (会破坏原列表中的 items)
        /// </summary>
        public static List<ILineInfo> SetBackgroundVocalsInfo(List<SyllableLineInfoWithSubLineState> list)
        {
            // 将已有背景人声行属性的歌词放入主歌词
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].IsBackgroundVocals == true)
                {
                    list[i - 1].SubLine = SyllableLineInfoWithSubLineState.GetSyllableLineInfo(list[i]);
                    list.RemoveAt(i--);
                }
            }

            // 判断没有预设属性的行
            bool IsNotBackgroundVocals(SyllableLineInfoWithSubLineState line) => line.IsBackgroundVocals is null && !line.IsBracketedLyrics || line.IsBackgroundVocals == false;
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].IsBackgroundVocals == null && list[i].IsBracketedLyrics) // 是符合背景人声条件的歌词行
                {
                    if (IsNotBackgroundVocals(list[i - 1]) && list[i - 1].SubLine is null) // 上一行符合主歌词行的条件
                    {
                        if (i >= list.Count || IsNotBackgroundVocals(list[i + 1])) // 下一行不是背景歌词行
                        {
                            list[i - 1].SubLine = SyllableLineInfoWithSubLineState.GetSyllableLineInfo(list[i]);
                            list.RemoveAt(i--);
                        }
                    }
                }
            }

            var lines = new List<ILineInfo>();
            // 移除 IsBackgroundVocals 属性
            for (int i = 0; i < list.Count; i++)
            {
                lines.Add(SyllableLineInfoWithSubLineState.GetSyllableLineInfo(list[i]));
            }
            return lines;
        }

        public class SyllableLineInfoWithSubLineState : SyllableLineInfo
        {
            public SyllableLineInfoWithSubLineState() { }

            public SyllableLineInfoWithSubLineState(IEnumerable<ISyllableInfo> syllables) : base(syllables) { }

            /// <summary>
            /// 是否是背景人声歌词
            /// </summary>
            public bool? IsBackgroundVocals { get; set; } = null;

            /// <summary>
            /// 头尾是括号的歌词
            /// </summary>
            public bool IsBracketedLyrics => (Text.StartsWith("(") || Text.StartsWith("（")) && (Text.EndsWith(")") || Text.EndsWith("）"));

            /// <summary>
            /// 创建一个 SyllableLineInfo 实例，以便与 SyllableLineInfoWithSubLineState 完全分离
            /// </summary>
            /// <param name="syllableLineInfoWithSubLineState"></param>
            /// <returns></returns>
            public static SyllableLineInfo GetSyllableLineInfo(SyllableLineInfoWithSubLineState syllableLineInfoWithSubLineState)
            {
                return new SyllableLineInfo(syllableLineInfoWithSubLineState.Syllables)
                {
                    LyricsAlignment = syllableLineInfoWithSubLineState.LyricsAlignment,
                    SubLine = syllableLineInfoWithSubLineState.SubLine,
                };
            }
        }

        /// <summary>
        /// 解析 Lyricify Syllable 歌词行
        /// </summary>
        public static SyllableLineInfoWithSubLineState? ParseLyricsLine(string line)
        {
            List<SyllableInfo> lyricItems = new();
            var lineInfo = new SyllableLineInfoWithSubLineState();

            if (line.IndexOf(']') != -1)
            {
                var properties = line[..line.IndexOf("]")];
                if (properties.Length > 1 && properties[1..].IsNumber())
                {
                    int p = int.Parse(properties[1..]);

                    // 读取预设的背景人声
                    if (p >= 6)
                    {
                        lineInfo.IsBackgroundVocals = true;
                    }
                    else if (p >= 3)
                    {
                        lineInfo.IsBackgroundVocals = false;
                    }

                    // 读取预设的对唱视图
                    switch (p % 3)
                    {
                        case 0:
                            lineInfo.LyricsAlignment = LyricsAlignment.Unspecified;
                            break;
                        case 1:
                            lineInfo.LyricsAlignment = LyricsAlignment.Left;
                            break;
                        case 2:
                            lineInfo.LyricsAlignment = LyricsAlignment.Right;
                            break;
                    }
                }
                line = line[(line.IndexOf("]") + 1)..];
            }

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

            lineInfo.Syllables = lyricItems.Cast<ISyllableInfo>().ToList();
            return lineInfo;
        }
    }
}
