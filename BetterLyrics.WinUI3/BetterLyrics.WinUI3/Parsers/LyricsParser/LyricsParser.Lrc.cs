using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        [GeneratedRegex(@"\[(\d*):(\d*)(\.|\:)(\d*)\]")]
        private static partial Regex LrcRegex();
        [GeneratedRegex(@"(\[|\<)(\d*):(\d*)\.(\d*)(\]|\>)([^\[\]\<\>]*)")]
        private static partial Regex SyllableRegex();

        private void ParseLrc(string raw)
        {
            var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            var lrcLines =
                new List<(int time, string text, List<(int time, string text)> syllables)>();

            // 支持 [mm:ss.xx]字、<mm:ss.xx>字，毫秒两位或三位
            var syllableRegex = SyllableRegex();

            foreach (var line in lines)
            {
                var matches = syllableRegex.Matches(line);
                var syllables = new List<(int, string)>();
                for (int i = 0; i < matches.Count; i++)
                {
                    var m = matches[i];
                    int min = int.Parse(m.Groups[2].Value);
                    int sec = int.Parse(m.Groups[3].Value);
                    int ms = int.Parse(m.Groups[4].Value.PadRight(3, '0'));
                    int totalMs = min * 60_000 + sec * 1000 + ms;
                    string text = m.Groups[6].Value;

                    syllables.Add((totalMs, text));
                }
                if (syllables.Count > 1)
                {
                    lrcLines.Add(
                        (
                            syllables[0].Item1,
                            string.Concat(syllables.Select(s => s.Item2)),
                            syllables
                        )
                    );
                }
                else
                {
                    // 普通LRC行
                    Regex? bracketRegex = LrcRegex();
                    var bracketMatches = bracketRegex.Matches(line);

                    string content = line;
                    int? lineStartTime = null;
                    if (bracketMatches.Count > 0)
                    {
                        var m = bracketMatches[0];
                        int min = int.Parse(m.Groups[1].Value);
                        int sec = int.Parse(m.Groups[2].Value);
                        int ms = int.Parse(m.Groups[4].Value.PadRight(3, '0'));
                        lineStartTime = min * 60_000 + sec * 1000 + ms;
                        content = bracketRegex!.Replace(line, "").Trim();
                        if (content == "//") content = "";
                        lrcLines.Add((lineStartTime.Value, content, new List<(int, string)>()));
                    }
                }
            }

            // 按时间分组
            var grouped = lrcLines.GroupBy(l => l.time).OrderBy(g => g.Key).ToList();
            int languageCount = 0;
            if (grouped != null && grouped.Count > 0)
            {
                // 计算最大语言数量
                languageCount = grouped.Max(g => g.Count());
            }

            // 初始化每种语言的歌词列表
            int langStartIndex = LyricsDataArr.Count;
            for (int i = 0; i < languageCount; i++) LyricsDataArr.Add(new LyricsData());

            // 遍历每个时间分组
            if (grouped != null)
            {
                foreach (var group in grouped)
                {
                    var linesInGroup = group.ToList();
                    for (int langIdx = 0; langIdx < languageCount; langIdx++)
                    {
                        // 只添加有对应行的语言，否则跳过
                        if (langIdx < linesInGroup.Count)
                        {
                            var (start, text, syllables) = linesInGroup[langIdx];
                            var line = new LyricsLine
                            {
                                StartMs = start,
                                OriginalText = text,
                                LyricsChars = [],
                            };
                            if (syllables != null && syllables.Count > 0)
                            {
                                int currentIndex = 0;
                                for (int j = 0; j < syllables.Count; j++)
                                {
                                    var (charStart, charText) = syllables[j];
                                    int startIndex = currentIndex;
                                    line.LyricsChars.Add(
                                        new LyricsChar
                                        {
                                            StartMs = charStart,
                                            Text = charText ?? "",
                                            StartIndex = startIndex,
                                        }
                                    );
                                    currentIndex += charText?.Length ?? 0;
                                }
                            }
                            LyricsDataArr[langStartIndex + langIdx].LyricsLines.Add(line);
                        }
                        // 没有翻译行则不补原文，直接跳过
                    }
                }
            }
        }

    }
}
