// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LyricsData = BetterLyrics.WinUI3.Models.LyricsData;

namespace BetterLyrics.WinUI3.Helper
{
    public class LyricsParser
    {
        private List<LyricsData> _lyricsDataArr = [];

        public List<LyricsData> Parse(string? raw, int? durationMs)
        {
            durationMs ??= (int)TimeSpan.FromMinutes(99).TotalMilliseconds;
            _lyricsDataArr = [];
            if (raw == null)
            {
                _lyricsDataArr.Add(LyricsData.GetNotfoundPlaceholder(durationMs.Value));
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
                        ParseQQNeteaseKugou(Lyricify.Lyrics.Parsers.QrcParser.Parse(raw).Lines);
                        break;
                    case LyricsFormat.Krc:
                        ParseQQNeteaseKugou(Lyricify.Lyrics.Parsers.KrcParser.Parse(raw).Lines);
                        break;
                    case LyricsFormat.Ttml:
                        ParseTtml(raw);
                        break;
                    default:
                        break;
                }
            }
            FillRomanizationLyricsData();
            _lyricsDataArr.Add(new LyricsData()); // 为机翻预留
            return _lyricsDataArr;
        }

        private void FillRomanizationLyricsData()
        {
            var chinese = _lyricsDataArr.Where(x => x.LanguageCode == "zh").FirstOrDefault();
            if (chinese != null)
            {
                _lyricsDataArr.Add(new LyricsData
                {
                    LanguageCode = "pinyin",
                    LyricsLines = chinese.LyricsLines.Select(line => new LyricsLine
                    {
                        StartMs = line.StartMs,
                        EndMs = line.EndMs,
                        OriginalText = Pinyin.Pinyin.Instance.HanziToPinyin(line.OriginalText).ToStr(),
                        LyricsChars = line.LyricsChars.Select(c => new LyricsChar
                        {
                            StartMs = c.StartMs,
                            EndMs = c.EndMs,
                            Text = Pinyin.Pinyin.Instance.HanziToPinyin(c.Text).ToStr(),
                            StartIndex = c.StartIndex
                        }).ToList()
                    }).ToList()
                });
                _lyricsDataArr.Add(new LyricsData
                {
                    LanguageCode = "jyutping",
                    LyricsLines = chinese.LyricsLines.Select(line => new LyricsLine
                    {
                        StartMs = line.StartMs,
                        EndMs = line.EndMs,
                        OriginalText = Pinyin.Jyutping.Instance.HanziToPinyin(line.OriginalText).ToStr(),
                        LyricsChars = line.LyricsChars.Select(c => new LyricsChar
                        {
                            StartMs = c.StartMs,
                            EndMs = c.EndMs,
                            Text = Pinyin.Jyutping.Instance.HanziToPinyin(c.Text).ToStr(),
                            StartIndex = c.StartIndex
                        }).ToList()
                    }).ToList()
                });
            }
            var japanese = _lyricsDataArr.Where(x => x.LanguageCode == "ja").FirstOrDefault();
            if (japanese != null)
            {
                _lyricsDataArr.Add(new LyricsData
                {
                    LanguageCode = "romaji",
                    LyricsLines = japanese.LyricsLines.Select(line => new LyricsLine
                    {
                        StartMs = line.StartMs,
                        EndMs = line.EndMs,
                        OriginalText = LanguageHelper.ToRomaji(line.OriginalText),
                        LyricsChars = line.LyricsChars.Select(c => new LyricsChar
                        {
                            StartMs = c.StartMs,
                            EndMs = c.EndMs,
                            Text = LanguageHelper.ToRomaji(c.Text),
                            StartIndex = c.StartIndex
                        }).ToList()
                    }).ToList()
                });
            }
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
            int languageCount = 0;
            if (grouped != null && grouped.Count > 0)
            {
                // 计算最大语言数量
                languageCount = grouped.Max(g => g.Count());
            }

            // 初始化每种语言的歌词列表
            _lyricsDataArr.Clear();
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
                            _lyricsDataArr[langIdx].LyricsLines.Add(line);
                        }
                        // 没有翻译行则不补原文，直接跳过
                    }
                }
            }
        }

        private void ParseTtml(string raw)
        {
            try
            {
                List<LyricsLine> originalLines = [];
                List<LyricsLine> translationLines = [];
                var xdoc = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);
                var body = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "body");
                if (body == null) return;
                var ps = body.Descendants().Where(e => e.Name.LocalName == "p");
                foreach (var p in ps)
                {
                    // 句级时间
                    string? pBegin = p.Attribute("begin")?.Value;
                    string? pEnd = p.Attribute("end")?.Value;
                    int pStartMs = ParseTtmlTime(pBegin);
                    int pEndMs = ParseTtmlTime(pEnd);

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

                    // 处理原文span后的空白
                    for (int i = 0; i < originalTextSpans.Count; i++)
                    {
                        var span = originalTextSpans[i];
                        var nextNode = span.NodesAfterSelf().FirstOrDefault();
                        if (nextNode is XText textNode)
                        {
                            span.Value += textNode.Value;
                        }
                    }
                    // 拼接空白字符后的原文
                    string originalText = string.Concat(originalTextSpans.Select(s => s.Value));

                    var originalCharTimings = new List<LyricsChar>();
                    int originalStartIndex = 0;
                    foreach (var span in originalTextSpans)
                    {
                        string? sBegin = span.Attribute("begin")?.Value;
                        string? sEnd = span.Attribute("end")?.Value;
                        int sStartMs = ParseTtmlTime(sBegin);
                        int sEndMs = ParseTtmlTime(sEnd);
                        originalCharTimings.Add(new LyricsChar
                        {
                            StartMs = sStartMs,
                            EndMs = sEndMs,
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
                        EndMs = pEndMs,
                        OriginalText = originalText,
                        LyricsChars = originalCharTimings,
                    });

                    // 翻译
                    string translationText = string.Concat(translationTextSpans.Select(s => s.Value));
                    var translationCharTimings = new List<LyricsChar>();
                    int translationStartIndex = 0;
                    foreach (var span in translationTextSpans)
                    {
                        string? sBegin = span.Attribute("begin")?.Value;
                        string? sEnd = span.Attribute("end")?.Value;
                        int sStartMs = ParseTtmlTime(sBegin);
                        int sEndMs = ParseTtmlTime(sEnd);
                        translationCharTimings.Add(new LyricsChar
                        {
                            StartMs = sStartMs,
                            EndMs = sEndMs,
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
                            EndMs = pEndMs,
                            OriginalText = translationText,
                            LyricsChars = translationCharTimings,
                        });
                    }
                }
                _lyricsDataArr.Add(new LyricsData(originalLines));
                if (translationLines.Count > 0)
                    _lyricsDataArr.Add(new LyricsData(translationLines));
            }
            catch
            {
                // 解析失败，忽略
            }
        }

        private static int ParseTtmlTime(string? t)
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

        private void ParseQQNeteaseKugou(List<ILineInfo>? lines)
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
                        EndMs = lineRead.EndTime ?? 0,
                        OriginalText = lineRead.Text,
                        LyricsChars = [],
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
                            var charTiming = new LyricsChar
                            {
                                StartMs = syllable.StartTime,
                                EndMs = syllable.EndTime,
                                Text = syllable.Text,
                                StartIndex = startIndex,
                            };
                            lineWrite.LyricsChars.Add(charTiming);
                            startIndex += syllable.Text.Length;
                        }
                    }

                    lyricsLines.Add(lineWrite);
                }
            }

            _lyricsDataArr.Add(new LyricsData(lyricsLines));
        }
    }
}
