using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        private void ParseTtml(string raw)
        {
            try
            {
                List<LyricsLine> originalLines = [];
                List<LyricsLine> translationLines = [];
                List<LyricsLine> romanLines = [];

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

                    // 只获取一级span
                    var spans = p.Elements()
                        .Where(s => s.Name.LocalName == "span")
                        .ToList();

                    var romanTextSpans = spans
                        .Where(s => s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value == "x-roman")
                        .ToList();
                    var originalTextSpans = spans
                        .Where(s => s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value == null)
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
                    {
                        originalText = p.Value;
                    }

                    originalLines.Add(new LyricsLine
                    {
                        StartMs = pStartMs,
                        EndMs = pEndMs,
                        OriginalText = originalText,
                        LyricsChars = originalCharTimings,
                    });

                    // 解析 x-role
                    ParseTtmlXRole(spans, translationLines, "x-translation", pStartMs, pEndMs);
                    ParseTtmlXRole(spans, romanLines, "x-roman", pStartMs, pEndMs);
                }

                LyricsDataArr.Add(new LyricsData(originalLines));
                if (translationLines.Count > 0)
                {
                    LyricsDataArr.Add(new LyricsData(translationLines));
                }
                if (romanLines.Count > 0)
                {
                    LyricsDataArr.Add(new LyricsData(romanLines) { LanguageCode = PhoneticHelper.RomanCode });
                }
            }
            catch
            {
                // 解析失败，忽略
            }
        }

        private void ParseTtmlXRole(List<XElement> sourceSpans, List<LyricsLine> saveLyricsLines, string xRole, int pStartMs, int? pEndMs)
        {
            var textSpans = sourceSpans
                .Where(s => s.Attribute(XName.Get("role", "http://www.w3.org/ns/ttml#metadata"))?.Value == xRole)
                .ToList();

            string text = string.Concat(textSpans.Select(s => s.Value));
            var charTimings = new List<LyricsChar>();
            int startIndex = 0;
            foreach (var span in textSpans)
            {
                string? sBegin = span.Attribute("begin")?.Value;
                string? sEnd = span.Attribute("end")?.Value;
                int sStartMs = ParseTtmlTime(sBegin);
                int sEndMs = ParseTtmlTime(sEnd);
                charTimings.Add(new LyricsChar
                {
                    StartMs = sStartMs,
                    EndMs = sEndMs,
                    StartIndex = startIndex,
                    Text = span.Value
                });
                startIndex += span.Value.Length;
            }
            if (textSpans.Count > 0)
            {
                saveLyricsLines.Add(new LyricsLine
                {
                    StartMs = pStartMs,
                    EndMs = pEndMs,
                    OriginalText = text,
                    LyricsChars = charTimings,
                });
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

    }
}
