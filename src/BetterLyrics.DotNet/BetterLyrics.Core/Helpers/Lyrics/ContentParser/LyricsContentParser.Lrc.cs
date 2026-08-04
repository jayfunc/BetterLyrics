using System.Text.RegularExpressions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.ContentParser;

public partial class LyricsContentParser
{
    [GeneratedRegex(@"\[(\d*):(\d*)(\.|\:)(\d*)\]")]
    private static partial Regex LrcRegex();

    [GeneratedRegex(@"(\[|\<)(\d*):(\d*)\.(\d*)(\]|\>)([^\[\]\<\>]*)")]
    private static partial Regex SyllableRegex();

    private void ParseLrc(string raw, LyricsProvider? provider = null, LyricsParseRule? rule = null)
    {
        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var lrcLines = new List<LyricsLine>();

        // 支持 [mm:ss.xx]字、<mm:ss.xx>字，毫秒两位或三位
        var syllableRegex = SyllableRegex();

        foreach (var line in lines)
        {
            var matches = syllableRegex.Matches(line);
            var syllables = new List<BaseLyrics>();

            var startIndex = 0;
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var min = int.Parse(match.Groups[2].Value);
                var sec = int.Parse(match.Groups[3].Value);
                var ms = int.Parse(match.Groups[4].Value.PadRight(3, '0'));
                var totalMs = min * 60_000 + sec * 1000 + ms;
                var text = match.Groups[6].Value;

                syllables.Add(new BaseLyrics { StartMs = totalMs, Text = text, StartIndex = startIndex });
                startIndex += text.Length;
            }

            if (syllables.Count > 1)
            {
                lrcLines.Add(new LyricsLine
                {
                    StartMs = syllables[0].StartMs,
                    PrimaryText = string.Concat(syllables.Select(s => s.Text)),
                    PrimarySyllables = syllables,
                    IsPrimaryHasRealSyllableInfo = true
                });
            }
            else
            {
                // 普通LRC行
                var bracketRegex = LrcRegex();
                var bracketMatches = bracketRegex.Matches(line);

                var content = line;
                int lineStartMs;
                if (bracketMatches.Count > 0)
                {
                    var match = bracketMatches[0];
                    var min = int.Parse(match.Groups[1].Value);
                    var sec = int.Parse(match.Groups[2].Value);
                    var ms = int.Parse(match.Groups[4].Value.PadRight(3, '0'));
                    lineStartMs = min * 60_000 + sec * 1000 + ms;

                    content = bracketRegex!.Replace(line, "").Trim();
                    if (content == "//") content = "";

                    var lyricsLine = new LyricsLine
                    {
                        StartMs = lineStartMs,
                        PrimaryText = content,
                        IsPrimaryHasRealSyllableInfo = false
                    };
                    lrcLines.Add(lyricsLine);
                }
            }
        }

        // 按时间分组
        var grouped = lrcLines.GroupBy(l => l.StartMs).OrderBy(g => g.Key).ToList();
        var languageCount = 0;
        if (grouped != null && grouped.Count > 0)
            // 计算最大语言数量
            languageCount = grouped.Max(g => g.Count());

        // 如果既没有开启翻译，也没有开启任何音译，那么只保留第一轨（原文）
        if (rule != null && !rule.IsTranslationEnabled && rule.AllowedRomanizationTags.Count == 0)
        {
            languageCount = Math.Min(languageCount, 1);
        }

        // 初始化每种语言的歌词列表
        var langStartIndex = LyricsDataArr.Count;
        for (var i = 0; i < languageCount; i++) AddLyricsData(new LyricsData { Provider = provider });

        // 遍历每个时间分组
        if (grouped != null)
            foreach (var group in grouped)
            {
                var linesInGroup = group.ToList();
                for (var langIdx = 0; langIdx < languageCount; langIdx++)
                    // 只添加有对应行的语言，否则跳过
                    if (langIdx < linesInGroup.Count)
                    {
                        var lyricsLine = linesInGroup[langIdx];
                        LyricsDataArr[langStartIndex + langIdx].LyricsLines.Add(lyricsLine);
                    }
                // 没有翻译行则不补原文，直接跳过
            }

        // 行数据填充完毕后，才能评估语言Tag并设置TrackType
        for (var i = 0; i < languageCount; i++)
        {
            var data = LyricsDataArr[langStartIndex + i];
            if (langStartIndex + i > 0 && data.TrackType == LyricsTrackType.Original)
            {
                data.TrackType = LanguageHelper.IsPhoneticTag(data.LanguageTag)
                    ? LyricsTrackType.Transliteration
                    : LyricsTrackType.Translation;
            }
        }
    }
}