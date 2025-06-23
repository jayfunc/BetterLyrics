// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="LyricsParser" />
    /// </summary>
    public class LyricsParser
    {
        #region Fields

        /// <summary>
        /// Defines the _multiLangLyricsLines
        /// </summary>
        private List<List<LyricsLine>> _multiLangLyricsLines = [];

        #endregion

        #region Methods

        /// <summary>
        /// The Parse
        /// </summary>
        /// <param name="raw">The raw<see cref="string"/></param>
        /// <param name="lyricsFormat">The lyricsFormat<see cref="LyricsFormat?"/></param>
        /// <param name="title">The title<see cref="string?"/></param>
        /// <param name="artist">The artist<see cref="string?"/></param>
        /// <param name="durationMs">The durationMs<see cref="int"/></param>
        /// <returns>The <see cref="List{List{LyricsLine}}"/></returns>
        public List<List<LyricsLine>> Parse(
            string raw,
            LyricsFormat? lyricsFormat = null,
            string? title = null,
            string? artist = null,
            int durationMs = 0
        )
        {
            _multiLangLyricsLines = [];
            switch (lyricsFormat)
            {
                case LyricsFormat.Lrc:
                case LyricsFormat.Eslrc:
                    ParseLrc(raw, durationMs);
                    break;
                case LyricsFormat.Ttml:
                    ParseTtml(raw, durationMs);
                    break;
                default:
                    break;
            }
            return _multiLangLyricsLines;
        }

        /// <summary>
        /// The ParseLrc
        /// </summary>
        /// <param name="raw">The raw<see cref="string"/></param>
        /// <param name="durationMs">The durationMs<see cref="int"/></param>
        private void ParseLrc(string raw, int durationMs)
        {
            var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var lrcLines =
                new List<(int time, string text, List<(int time, string text)> syllables)>();

            // 支持 [mm:ss.xx]字、<mm:ss.xx>字，毫秒两位或三位
            var syllableRegex = new Regex(
                @"(\[|\<)(\d{2}):(\d{2})\.(\d{2,3})(\]|\>)([^\[\]\<\>]*)"
            );

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
                if (syllables.Count > 0)
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
                    var bracketRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]");
                    var bracketMatches = bracketRegex.Matches(line);
                    string content = line;
                    int? lineStartTime = null;
                    if (bracketMatches.Count > 0)
                    {
                        var m = bracketMatches[0];
                        int min = int.Parse(m.Groups[1].Value);
                        int sec = int.Parse(m.Groups[2].Value);
                        int ms = int.Parse(m.Groups[3].Value.PadRight(3, '0'));
                        lineStartTime = min * 60_000 + sec * 1000 + ms;
                        content = bracketRegex.Replace(line, "").Trim();
                        lrcLines.Add((lineStartTime.Value, content, new List<(int, string)>()));
                    }
                }
            }

            // 按时间分组
            var grouped = lrcLines.GroupBy(l => l.time).OrderBy(g => g.Key).ToList();
            int languageCount = grouped.Max(g => g.Count());

            // 初始化每种语言的歌词列表
            _multiLangLyricsLines.Clear();
            for (int i = 0; i < languageCount; i++)
                _multiLangLyricsLines.Add(new List<LyricsLine>());

            // 遍历每个时间分组
            foreach (var group in grouped)
            {
                var linesInGroup = group.ToList();
                for (int langIdx = 0; langIdx < languageCount; langIdx++)
                {
                    // 如果该语言有翻译，取对应行，否则用原文（第一行）
                    var (start, text, syllables) =
                        langIdx < linesInGroup.Count ? linesInGroup[langIdx] : linesInGroup[0];
                    var line = new LyricsLine
                    {
                        StartMs = start,
                        EndMs = 0, // 稍后统一修正
                        Text = text,
                        CharTimings = [],
                    };
                    if (syllables != null && syllables.Count > 0)
                    {
                        for (int j = 0; j < syllables.Count; j++)
                        {
                            var (charStart, charText) = syllables[j];
                            int charEnd = (j + 1 < syllables.Count) ? syllables[j + 1].Item1 : 0;
                            line.CharTimings.Add(
                                new CharTiming { StartMs = charStart, EndMs = charEnd }
                            );
                        }
                    }
                    _multiLangLyricsLines[langIdx].Add(line);
                }
            }

            // 修正 EndMs
            for (int langIdx = 0; langIdx < languageCount; langIdx++)
            {
                var linesInSingleLang = _multiLangLyricsLines[langIdx];
                for (int i = 0; i < linesInSingleLang.Count; i++)
                {
                    if (i + 1 < linesInSingleLang.Count)
                        linesInSingleLang[i].EndMs = linesInSingleLang[i + 1].StartMs;
                    else
                        linesInSingleLang[i].EndMs = durationMs;

                    // 修正 CharTimings 的最后一个 EndMs
                    var timings = linesInSingleLang[i].CharTimings;
                    if (timings.Count > 0)
                    {
                        for (int j = 0; j < timings.Count; j++)
                        {
                            if (j + 1 < timings.Count)
                                timings[j].EndMs = timings[j + 1].StartMs;
                            else
                                timings[j].EndMs = linesInSingleLang[i].EndMs;
                        }
                    }
                }
                PostProcessLyricsLines(linesInSingleLang);
            }
        }

        /// <summary>
        /// The ParseTtml
        /// </summary>
        /// <param name="raw">The raw<see cref="string"/></param>
        /// <param name="durationMs">The durationMs<see cref="int"/></param>
        private void ParseTtml(string raw, int durationMs)
        {
            try
            {
                List<LyricsLine> singleLangLyricsLine = [];
                var xdoc = XDocument.Parse(raw);
                var body = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "body");
                if (body == null)
                    return;
                var ps = body.Descendants().Where(e => e.Name.LocalName == "p");
                foreach (var p in ps)
                {
                    // 句级时间
                    string? pBegin = p.Attribute("begin")?.Value;
                    string? pEnd = p.Attribute("end")?.Value;
                    int pStartMs = ParseTtmlTime(pBegin);
                    int pEndMs = ParseTtmlTime(pEnd);

                    // 处理分词分时
                    var spans = p.Elements()
                        .Where(s =>
                            s.Name.LocalName == "span"
                            && s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))
                                == null
                        )
                        .ToList();

                    string text = string.Concat(spans.Select(s => s.Value));
                    var charTimings = new List<CharTiming>();

                    for (int i = 0; i < spans.Count; i++)
                    {
                        var span = spans[i];
                        string? sBegin = span.Attribute("begin")?.Value;
                        string? sEnd = span.Attribute("end")?.Value;
                        int sStartMs = ParseTtmlTime(sBegin);
                        int sEndMs = ParseTtmlTime(sEnd);

                        if (sStartMs == 0 && sEndMs == 0)
                            continue;

                        if (sEndMs == 0)
                            sEndMs =
                                (i + 1 < spans.Count)
                                    ? ParseTtmlTime(spans[i + 1].Attribute("begin")?.Value)
                                    : pEndMs;

                        charTimings.Add(new CharTiming { StartMs = sStartMs, EndMs = sEndMs });
                    }

                    if (spans.Count == 0)
                        text = p.Value.Trim();

                    singleLangLyricsLine.Add(
                        new LyricsLine
                        {
                            StartMs = pStartMs,
                            EndMs = pEndMs,
                            Text = text,
                            CharTimings = charTimings,
                        }
                    );
                }
                PostProcessLyricsLines(singleLangLyricsLine);
                _multiLangLyricsLines.Add(singleLangLyricsLine);
            }
            catch
            {
                // 解析失败，忽略
            }
        }

        /// <summary>
        /// The ParseTtmlTime
        /// </summary>
        /// <param name="t">The t<see cref="string?"/></param>
        /// <returns>The <see cref="int"/></returns>
        private int ParseTtmlTime(string? t)
        {
            if (string.IsNullOrWhiteSpace(t))
                return 0;

            t = t.Trim();

            // 支持 "1.000s"
            if (t.EndsWith("s"))
            {
                if (
                    double.TryParse(
                        t.TrimEnd('s'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double seconds
                    )
                )
                    return (int)(seconds * 1000);
            }
            else
            {
                var parts = t.Split(':');
                if (parts.Length == 3)
                {
                    // hh:mm:ss.xxx
                    int h = int.Parse(parts[0]);
                    int m = int.Parse(parts[1]);
                    double s = double.Parse(
                        parts[2],
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    return (int)((h * 3600 + m * 60 + s) * 1000);
                }
                else if (parts.Length == 2)
                {
                    // mm:ss.xxx
                    int m = int.Parse(parts[0]);
                    double s = double.Parse(
                        parts[1],
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    return (int)((m * 60 + s) * 1000);
                }
                else if (parts.Length == 1)
                {
                    // ss.xxx
                    if (
                        double.TryParse(
                            parts[0],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double s
                        )
                    )
                        return (int)(s * 1000);
                }
            }
            return 0;
        }

        /// <summary>
        /// The PostProcessLyricsLines
        /// </summary>
        /// <param name="lines">The lines<see cref="List{LyricsLine}"/></param>
        private void PostProcessLyricsLines(List<LyricsLine> lines)
        {
            if (lines.Count > 0 && lines[0].StartMs > 0)
            {
                lines.Insert(
                    0,
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = lines[0].StartMs,
                        Text = "",
                        CharTimings = [],
                    }
                );
            }
        }

        #endregion
    }
}
