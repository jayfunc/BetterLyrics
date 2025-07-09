// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.Globalization.Fonts;

namespace BetterLyrics.WinUI3.Helper
{
    public class LyricsParser
    {
        private List<List<LyricsLine>> _multiLangLyricsLines = [];

        public List<List<LyricsLine>> Parse(string? raw, int durationMs)
        {
            _multiLangLyricsLines = [];
            if (raw == null)
            {
                _multiLangLyricsLines.Add(
                    [
                        new LyricsLine
                        {
                            StartMs = 0,
                            EndMs = durationMs,
                            OriginalText = App.ResourceLoader!.GetString("LyricsNotFound"),
                            CharTimings = [],
                        },
                    ]
                );
            }
            else
            {
                switch (raw.DetectFormat())
                {
                    case LyricsFormat.Lrc:
                    case LyricsFormat.Eslrc:
                        ParseLrc(raw);
                        break;
                    case LyricsFormat.Qrc:
                        ParseUsingLyricify(Lyricify.Lyrics.Parsers.QrcParser.Parse(raw).Lines);
                        break;
                    case LyricsFormat.Krc:
                        ParseUsingLyricify(Lyricify.Lyrics.Parsers.KrcParser.Parse(raw).Lines);
                        break;
                    case LyricsFormat.Ttml:
                        ParseTtml(raw);
                        break;
                    default:
                        break;
                }
            }
            PostProcessLyricsLines(durationMs);
            return _multiLangLyricsLines;
        }

        private void ParseLrc(string raw)
        {
            var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
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
                        content = bracketRegex.Replace(line, "");
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
                        OriginalText = text,
                        CharTimings = [],
                    };
                    if (syllables != null && syllables.Count > 0)
                    {
                        int currentIndex = 0;
                        for (int j = 0; j < syllables.Count; j++)
                        {
                            var (charStart, charText) = syllables[j];
                            int startIndex = currentIndex;
                            line.CharTimings.Add(
                                new CharTiming
                                {
                                    StartMs = charStart,
                                    EndMs = 0, // Fixed later
                                    Text = charText ?? "",
                                    StartIndex = startIndex,
                                }
                            );
                            currentIndex += charText?.Length ?? 0;
                        }
                    }
                    _multiLangLyricsLines[langIdx].Add(line);
                }
            }
        }

        private void ParseTtml(string raw)
        {
            try
            {
                List<LyricsLine> originalLines = [];
                List<LyricsLine> translationLines = [];
                var xdoc = XDocument.Parse(raw);
                var body = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "body");
                if (body == null) return;
                var ps = body.Descendants().Where(e => e.Name.LocalName == "p");
                foreach (var p in ps)
                {
                    // 句级时间
                    string? pBegin = p.Attribute("begin")?.Value;
                    int pStartMs = ParseTtmlTime(pBegin);

                    // 只获取一级span，且排除ttm:role="x-bg"的span
                    var spans = p.Elements()
                        .Where(s => s.Name.LocalName == "span" &&
                                    s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value != "x-bg")
                        .ToList();

                    // 原文和翻译分离
                    var originalTextSpans = spans
                        .Where(s => s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value != "x-translation")
                        .ToList();
                    var translationTextSpans = spans
                        .Where(s => s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value == "x-translation")
                        .ToList();

                    // 原文（非 CJK 语言添加空格）
                    string originalText = string.Concat(originalTextSpans.Select(s => s.Value));
                    if (!LanguageDetectionHelper.IsCJK(originalText))
                    {
                        foreach (var span in originalTextSpans)
                        {
                            span.Value += " ";
                        }
                        originalText = string.Concat(originalTextSpans.Select(s => s.Value));
                    }

                    var originalCharTimings = new List<CharTiming>();
                    int originalStartIndex = 0;
                    foreach (var span in originalTextSpans)
                    {
                        string? sBegin = span.Attribute("begin")?.Value;
                        int sStartMs = ParseTtmlTime(sBegin);
                        originalCharTimings.Add(new CharTiming
                        {
                            StartMs = sStartMs,
                            EndMs = 0,
                            StartIndex = originalStartIndex,
                            Text = span.Value
                        });
                        originalStartIndex += span.Value.Length;
                    }
                    if (originalTextSpans.Count == 0)
                        originalText = p.Value;

                    originalLines.Add(new LyricsLine
                    {
                        StartMs = pStartMs,
                        EndMs = 0,
                        OriginalText = originalText,
                        CharTimings = originalCharTimings,
                    });

                    // 翻译
                    string translationText = string.Concat(translationTextSpans.Select(s => s.Value));
                    var translationCharTimings = new List<CharTiming>();
                    int translationStartIndex = 0;
                    foreach (var span in translationTextSpans)
                    {
                        string? sBegin = span.Attribute("begin")?.Value;
                        int sStartMs = ParseTtmlTime(sBegin);
                        translationCharTimings.Add(new CharTiming
                        {
                            StartMs = sStartMs,
                            EndMs = 0,
                            StartIndex = translationStartIndex,
                            Text = span.Value
                        });
                        translationStartIndex += span.Value.Length;
                    }
                    if (translationTextSpans.Count > 0)
                    {
                        translationLines.Add(new LyricsLine
                        {
                            StartMs = pStartMs,
                            EndMs = 0,
                            OriginalText = translationText,
                            CharTimings = translationCharTimings,
                        });
                    }
                }
                _multiLangLyricsLines.Add(originalLines);
                if (translationLines.Count > 0)
                    _multiLangLyricsLines.Add(translationLines);
            }
            catch
            {
                // 解析失败，忽略
            }
        }

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

        private void ParseUsingLyricify(List<ILineInfo>? lines)
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
                        EndMs = 0,
                        OriginalText = lineRead.Text,
                        CharTimings = [],
                    };

                    var syllables = (lineRead as SyllableLineInfo)?.Syllables;
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
                            var charTiming = new CharTiming
                            {
                                StartMs = syllable.StartTime,
                                EndMs = 0,
                                Text = syllable.Text,
                                StartIndex = startIndex,
                            };
                            if (syllableIndex + 1 < syllables.Count)
                            {
                                charTiming.EndMs = syllables[syllableIndex + 1].StartTime;
                            }
                            else
                            {
                                charTiming.EndMs = lineWrite.EndMs;
                            }
                            lineWrite.CharTimings.Add(charTiming);
                            startIndex += syllable.Text.Length;
                        }
                    }

                    lyricsLines.Add(lineWrite);
                }
            }

            _multiLangLyricsLines.Add(lyricsLines);
        }

        private void PostProcessLyricsLines(int durationMs)
        {
            for (int langIdx = 0; langIdx < _multiLangLyricsLines.Count; langIdx++)
            {
                var linesInSingleLang = _multiLangLyricsLines[langIdx];
                for (int i = 0; i < linesInSingleLang.Count; i++)
                {
                    if (i + 1 < linesInSingleLang.Count)
                    {
                        linesInSingleLang[i].EndMs = linesInSingleLang[i + 1].StartMs;
                    }
                    else
                    {
                        linesInSingleLang[i].EndMs = durationMs;
                    }

                    // 修正 CharTimings 的 EndMs
                    var timings = linesInSingleLang[i].CharTimings;
                    if (timings.Count > 0)
                    {
                        for (int j = 0; j < timings.Count; j++)
                        {
                            if (j + 1 < timings.Count)
                            {
                                timings[j].EndMs = timings[j + 1].StartMs;
                            }
                            else
                            {
                                timings[j].EndMs = linesInSingleLang[i].EndMs;
                            }
                        }
                    }
                }
                if (linesInSingleLang.Count > 0 && linesInSingleLang[0].StartMs > 0)
                {
                    linesInSingleLang.Insert(
                        0,
                        new LyricsLine
                        {
                            StartMs = 0,
                            EndMs = linesInSingleLang[0].StartMs,
                            OriginalText = "● ● ●",
                            CharTimings = [],
                        }
                    );
                }
            }
        }
    }
}
