using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        [GeneratedRegex(@"\[(\d*):(\d*)(\.|\:)(\d*)\]")]
        private static partial Regex LrcRegex();
        [GeneratedRegex(@"(\[|\<)(\d*):(\d*)\.(\d*)(\]|\>)([^\[\]\<\>]*)")]
        private static partial Regex SyllableRegex();

        private void ParseLrc(string raw, bool single)
        {
            var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            var lrcLines = new List<LyricsLine>();

            // 支持 [mm:ss.xx]字、<mm:ss.xx>字，毫秒两位或三位
            var syllableRegex = SyllableRegex();

            foreach (var line in lines)
            {
                var matches = syllableRegex.Matches(line);
                var syllables = new List<LyricsSyllable>();

                int startIndex = 0;
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    int min = int.Parse(match.Groups[2].Value);
                    int sec = int.Parse(match.Groups[3].Value);
                    int ms = int.Parse(match.Groups[4].Value.PadRight(3, '0'));
                    int totalMs = min * 60_000 + sec * 1000 + ms;
                    string text = match.Groups[6].Value;

                    syllables.Add(new LyricsSyllable { StartMs = totalMs, Text = text, StartIndex = startIndex });
                    startIndex += text.Length;
                }

                int lineEndMs = 0;

                if (syllables.Count > 0)
                {
                    var lastSyllable = syllables[syllables.Count - 1];
                    if (string.IsNullOrWhiteSpace(lastSyllable.Text))
                    {
                        lineEndMs = lastSyllable.StartMs;
                        syllables.RemoveAt(syllables.Count - 1);
                    }
                }

                if (syllables.Count > 0)
                {
                    lrcLines.Add(new LyricsLine
                    {
                        StartMs = syllables[0].StartMs,
                        EndMs = lineEndMs,
                        OriginalText = string.Concat(syllables.Select(s => s.Text)),
                        LyricsSyllables = syllables
                    });
                }
                else
                {
                    // 普通LRC行
                    Regex? bracketRegex = LrcRegex();
                    var bracketMatches = bracketRegex.Matches(line);

                    string content = line;
                    int lineStartMs;
                    if (bracketMatches.Count > 0)
                    {
                        var match = bracketMatches[0];
                        int min = int.Parse(match.Groups[1].Value);
                        int sec = int.Parse(match.Groups[2].Value);
                        int ms = int.Parse(match.Groups[4].Value.PadRight(3, '0'));
                        lineStartMs = min * 60_000 + sec * 1000 + ms;

                        content = bracketRegex!.Replace(line, "").Trim();
                        if (content == "//") content = "";

                        lrcLines.Add(new LyricsLine { StartMs = lineStartMs, OriginalText = content });
                    }
                }
            }

            if (single)
            {
                _lyricsDataArr.Add(new LyricsData(lrcLines));
            }
            else
            {
                // 按时间分组
                var grouped = lrcLines.GroupBy(l => l.StartMs).OrderBy(g => g.Key).ToList();
                int languageCount = 0;
                if (grouped != null && grouped.Count > 0)
                {
                    // 计算最大语言数量
                    languageCount = grouped.Max(g => g.Count());
                }

                // 初始化每种语言的歌词列表
                int langStartIndex = _lyricsDataArr.Count;
                for (int i = 0; i < languageCount; i++) _lyricsDataArr.Add(new LyricsData());

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
                                var lyricsLine = linesInGroup[langIdx];
                                _lyricsDataArr[langStartIndex + langIdx].LyricsLines.Add(lyricsLine);
                            }
                            // 没有翻译行则不补原文，直接跳过
                        }
                    }
                }
            }
        }
    }
}
