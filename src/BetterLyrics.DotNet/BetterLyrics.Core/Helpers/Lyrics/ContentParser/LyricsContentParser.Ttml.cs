using System.Globalization;
using System.Text;
using System.Xml.Linq;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.ContentParser;

/// <summary>
///     This TTML content parser follows the format specification:
///     https://github.com/amll-dev/amll-ttml-db/wiki/%E6%A0%BC%E5%BC%8F%E8%A7%84%E8%8C%83
/// </summary>
public partial class LyricsContentParser
{
    private readonly XNamespace _itunes = "http://itunes.apple.com/lyric-ttml-extensions";
    private readonly XNamespace _ttml = "http://www.w3.org/ns/ttml#metadata";
    private readonly XNamespace _tts = "http://www.w3.org/ns/ttml#styling";

    private void ParseTtml(string raw)
    {
        try
        {
            List<LyricsLine> originalLines = [];
            List<LyricsLine> translationLines = [];
            List<LyricsLine> romanLines = [];

            var xdoc = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);

            // 预解析头部的 Apple Music 扩展辅助轨道数据
            Dictionary<string, List<XElement>> headTransDict = [];
            Dictionary<string, List<XElement>> headRomanDict = [];

            var head = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "head");
            if (head != null)
            {
                var texts = head.Descendants().Where(e => e.Name.LocalName == "text");
                foreach (var text in texts)
                {
                    var forKey = text.Attribute("for")?.Value;
                    if (string.IsNullOrEmpty(forKey)) continue;

                    var grandParent = text.Parent?.Parent?.Name.LocalName;
                    if (grandParent == "translations")
                    {
                        if (!headTransDict.ContainsKey(forKey)) headTransDict[forKey] = [];
                        headTransDict[forKey].Add(text);
                    }
                    else if (grandParent == "transliterations")
                    {
                        if (!headRomanDict.ContainsKey(forKey)) headRomanDict[forKey] = [];
                        headRomanDict[forKey].Add(text);
                    }
                }
            }

            // 解析正文
            var body = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "body");
            if (body == null) return;

            var ps = body.Descendants().Where(e => e.Name.LocalName == "p");

            foreach (var p in ps)
            {
                var pKey = p.Attribute(_itunes + "key")?.Value ?? "";
                var agentId = p.Attribute(_ttml + "agent")?.Value ?? "";

                // 解析主歌词行
                ParseTtmlSegment(
                    p,
                    originalLines,
                    translationLines,
                    romanLines,
                    agentId
                );

                var currentOriginalLine = originalLines.LastOrDefault();
                var pStart = currentOriginalLine?.StartMs ?? 0;
                var pEnd = currentOriginalLine?.EndMs ?? 0;

                // Apple Music 扩展轨道注入
                if (!string.IsNullOrEmpty(pKey))
                {
                    if (headTransDict.TryGetValue(pKey, out var transTexts))
                        foreach (var tText in transTexts)
                        {
                            ParseTtmlSegment(tText, translationLines, null, null, agentId, pStart, pEnd);

                            // 处理可能嵌套在扩展 text 中的背景人声
                            var textBgSpans = tText.Elements().Where(s => s.Attribute(_ttml + "role")?.Value == "x-bg");
                            foreach (var bg in textBgSpans)
                                ParseTtmlSegment(bg, translationLines, null, null, agentId, pStart, pEnd);
                        }

                    if (headRomanDict.TryGetValue(pKey, out var romanTexts))
                        foreach (var rText in romanTexts)
                        {
                            ParseTtmlSegment(rText, romanLines, null, null, agentId, pStart, pEnd);

                            var textBgSpans = rText.Elements().Where(s => s.Attribute(_ttml + "role")?.Value == "x-bg");
                            foreach (var bg in textBgSpans)
                                ParseTtmlSegment(bg, romanLines, null, null, agentId, pStart, pEnd);
                        }
                }

                // 行内嵌的背景人声
                var bgSpans = p.Elements().Where(s => s.Attribute(_ttml + "role")?.Value == "x-bg");
                foreach (var bgSpan in bgSpans)
                    ParseTtmlSegment(
                        bgSpan,
                        originalLines,
                        translationLines,
                        romanLines,
                        fallbackStartMs: pStart,
                        fallbackEndMs: pEnd,
                        agentId: agentId
                    );
            }

            _lyricsDataArr.Add(new LyricsData(originalLines));

            if (translationLines.Count > 0) _lyricsDataArr.Add(new LyricsData(translationLines));

            if (romanLines.Count > 0)
                _lyricsDataArr.Add(new LyricsData(romanLines) { LanguageCode = LanguageHelper.RomanCode });
        }
        catch
        {
        }
    }

    private void ParseTtmlSegment(
        XElement container,
        List<LyricsLine>? primaryDest,
        List<LyricsLine>? transDest,
        List<LyricsLine>? romanDest,
        string agentId,
        int fallbackStartMs = 0,
        int fallbackEndMs = 0)
    {
        var startMs = fallbackStartMs;
        var beginAttr = container.Attribute("begin");
        if (beginAttr != null) startMs = ParseTtmlTime(beginAttr.Value);

        int? endMs = fallbackEndMs;
        var endAttr = container.Attribute("end");
        if (endAttr != null) endMs = ParseTtmlTime(endAttr.Value);

        var syllables = new List<BaseLyrics>();
        var sbText = new StringBuilder();
        var startIndex = 0;

        // 用于追踪上一个被添加到列表的音节，以便将后续的空格或标点追加给它
        BaseLyrics? lastSyllable = null;

        // 遍历节点，提取纯文本与音节时轴
        foreach (var node in container.Nodes())
            if (node is XText xText)
            {
                var textVal = xText.Value;

                // 规范 3.3 兜底：只要包含换行符，说明这是 XML 排版格式化，丢弃多余的空白字符。
                // 这样可以避免把编辑器里的换行缩进当成歌词空格解析进去。
                if (textVal.Contains('\n')) textVal = textVal.Trim(' ', '\t', '\r', '\n');

                if (string.IsNullOrEmpty(textVal)) continue;

                // 核心修复：如果纯文本节点（如行内空格、逗号）在 span 之后出现，追加到上一个音节的文本末尾
                if (lastSyllable != null) lastSyllable.Text += textVal;

                sbText.Append(textVal);
                startIndex += textVal.Length;
            }
            else if (node is XElement xElement && xElement.Name.LocalName == "span")
            {
                var role = xElement.Attribute(_ttml + "role")?.Value;
                // 剔除功能性子节点，它们会在外部独立解析
                if (role == "x-bg" || role == "x-translation" || role == "x-roman") continue;

                var rubyAttr = xElement.Attribute(_tts + "ruby")?.Value;
                var textVal = "";
                var sStartMs = startMs;
                var sEndMs = endMs;

                if (rubyAttr == "container")
                {
                    var baseSpan = xElement.Elements().FirstOrDefault(e => e.Attribute(_tts + "ruby")?.Value == "base");
                    var textSpans = xElement.Descendants().Where(e => e.Attribute(_tts + "ruby")?.Value == "text")
                        .ToList();

                    textVal = baseSpan?.Value ?? "";
                    var firstTime = ParseTtmlTime(textSpans.FirstOrDefault()?.Attribute("begin")?.Value ??
                                                  xElement.Attribute("begin")?.Value);
                    var lastTime = ParseTtmlTime(textSpans.LastOrDefault()?.Attribute("end")?.Value ??
                                                 xElement.Attribute("end")?.Value);

                    sStartMs = firstTime != 0 ? firstTime : startMs;
                    sEndMs = lastTime != 0 ? lastTime : endMs;
                }
                else
                {
                    // 包含在 span 内的文本（含自带尾随空格）将被原样提取
                    textVal = xElement.Value;
                    var bTime = ParseTtmlTime(xElement.Attribute("begin")?.Value);
                    var eTime = ParseTtmlTime(xElement.Attribute("end")?.Value);

                    sStartMs = bTime != 0 ? bTime : startMs;
                    sEndMs = eTime != 0 ? eTime : endMs;
                }

                if (!string.IsNullOrEmpty(textVal))
                {
                    var syl = new BaseLyrics
                    {
                        StartMs = sStartMs,
                        EndMs = sEndMs,
                        StartIndex = startIndex,
                        Text = textVal
                    };
                    syllables.Add(syl);

                    // 更新 lastSyllable 指针
                    lastSyllable = syl;

                    sbText.Append(textVal);
                    startIndex += textVal.Length;
                }
            }

        var fullPrimaryText = sbText.ToString().Trim();

        // 容器若缺起止时间，使用音节时间进行兜底补全
        if (beginAttr == null && syllables.Count > 0) startMs = syllables.First().StartMs;
        if (endAttr == null && syllables.Count > 0) endMs = syllables.Last().EndMs;

        if (!string.IsNullOrWhiteSpace(fullPrimaryText) && primaryDest != null)
            primaryDest.Add(new LyricsLine
            {
                StartMs = startMs,
                EndMs = endMs,
                PrimaryText = fullPrimaryText,
                PrimarySyllables = syllables,
                IsPrimaryHasRealSyllableInfo = syllables.Count > 0,
                AgentId = agentId
            });

        // 行内嵌的翻译及罗马音
        if (transDest != null)
        {
            var transSpan = container.Elements()
                .FirstOrDefault(s => s.Attribute(_ttml + "role")?.Value == "x-translation");
            AddAuxiliaryLine(transDest, transSpan, startMs, endMs);
        }

        if (romanDest != null)
        {
            var romanSpan = container.Elements().FirstOrDefault(s => s.Attribute(_ttml + "role")?.Value == "x-roman");
            AddAuxiliaryLine(romanDest, romanSpan, startMs, endMs);
        }
    }

    private void AddAuxiliaryLine(List<LyricsLine> destList, XElement? span, int startMs, int? endMs)
    {
        if (span != null && !string.IsNullOrWhiteSpace(span.Value))
            destList.Add(new LyricsLine
            {
                StartMs = startMs,
                EndMs = endMs,
                PrimaryText = span.Value.Trim(),
                IsPrimaryHasRealSyllableInfo = false
            });
    }

    private static int ParseTtmlTime(string? t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return 0;

        t = t.Trim();
        var parts = t.Split(':');

        try
        {
            if (parts.Length == 3)
            {
                // hh:mm:ss.xxx
                var h = int.Parse(parts[0]);
                var m = int.Parse(parts[1]);
                var s = double.Parse(parts[2], CultureInfo.InvariantCulture);
                return (int)((h * 3600 + m * 60 + s) * 1000);
            }

            if (parts.Length == 2)
            {
                // mm:ss.xxx
                var m = int.Parse(parts[0]);
                var s = double.Parse(parts[1], CultureInfo.InvariantCulture);
                return (int)((m * 60 + s) * 1000);
            }

            if (parts.Length == 1)
            {
                // ss.xxx
                var s = double.Parse(parts[0], CultureInfo.InvariantCulture);
                return (int)(s * 1000);
            }
        }
        catch
        {
        }

        return 0;
    }
}