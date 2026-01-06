using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        private readonly XNamespace _ttml = "http://www.w3.org/ns/ttml#metadata";

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
                    ParseTtmlSegment(
                        container: p,
                        originalDest: originalLines,
                        transDest: translationLines,
                        romanDest: romanLines,
                        isBackground: false
                    );

                    var bgSpans = p.Elements().Where(s => s.Attribute(_ttml + "role")?.Value == "x-bg");

                    foreach (var bgSpan in bgSpans)
                    {
                        // 把 span 当作一个容器，再调一次通用解析方法
                        ParseTtmlSegment(
                            container: bgSpan,
                            originalDest: originalLines,
                            transDest: translationLines,
                            romanDest: romanLines,
                            isBackground: true
                        );
                    }
                }

                _lyricsDataArr.Add(new LyricsData(originalLines));

                if (translationLines.Count > 0)
                {
                    _lyricsDataArr.Add(new LyricsData(translationLines));
                }

                if (romanLines.Count > 0)
                {
                    _lyricsDataArr.Add(new LyricsData(romanLines) { LanguageCode = PhoneticHelper.RomanCode });
                }
            }
            catch { }
        }

        private void ParseTtmlSegment(
            XElement container,
            List<LyricsLine> originalDest,
            List<LyricsLine> transDest,
            List<LyricsLine> romanDest,
            bool isBackground)
        {
            int containerStartMs = ParseTtmlTime(container.Attribute("begin")?.Value);
            int containerEndMs = ParseTtmlTime(container.Attribute("end")?.Value);

            var contentSpans = container.Elements()
                .Where(s => s.Name.LocalName == "span")
                .Where(s =>
                {
                    var role = s.Attribute(_ttml + "role")?.Value;
                    return role == null;
                })
                .ToList();

            for (int i = 0; i < contentSpans.Count; i++)
            {
                var span = contentSpans[i];
                var nextNode = span.NodesAfterSelf().FirstOrDefault();
                if (nextNode is XText textNode)
                {
                    span.Value += textNode.Value;
                }
            }

            var syllables = new List<LyricsSyllable>();
            int startIndex = 0;
            var sbText = new System.Text.StringBuilder();

            foreach (var span in contentSpans)
            {
                int sStartMs = ParseTtmlTime(span.Attribute("begin")?.Value);
                int sEndMs = ParseTtmlTime(span.Attribute("end")?.Value);
                string text = span.Value;

                syllables.Add(new LyricsSyllable
                {
                    StartMs = sStartMs,
                    EndMs = sEndMs,
                    StartIndex = startIndex,
                    Text = text
                });

                sbText.Append(text);
                startIndex += text.Length;
            }

            string fullOriginalText = sbText.ToString();

            if (contentSpans.Count == 0)
            {
                fullOriginalText = container.Value;
            }

            originalDest.Add(new LyricsLine
            {
                StartMs = containerStartMs,
                EndMs = containerEndMs,
                OriginalText = fullOriginalText,
                LyricsSyllables = syllables
            });

            var transSpan = container.Elements()
                .FirstOrDefault(s => s.Attribute(_ttml + "role")?.Value == "x-translation");

            AddAuxiliaryLine(transDest, transSpan, containerStartMs, containerEndMs);

            var romanSpan = container.Elements()
                .FirstOrDefault(s => s.Attribute(_ttml + "role")?.Value == "x-roman");

            AddAuxiliaryLine(romanDest, romanSpan, containerStartMs, containerEndMs);
        }

        private void AddAuxiliaryLine(List<LyricsLine> destList, XElement? span, int startMs, int endMs)
        {
            if (span != null)
            {
                string text = span.Value;

                destList.Add(new LyricsLine
                {
                    StartMs = startMs,
                    EndMs = endMs,
                    OriginalText = text
                });
            }
            else
            {
                destList.Add(new LyricsLine
                {
                    StartMs = startMs,
                    EndMs = endMs,
                    OriginalText = ""
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
