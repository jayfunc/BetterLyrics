using Lyricify.Lyrics.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Lyricify.Lyrics.Parsers
{
    /// <summary>
    /// Apple Music TTML parser
    /// </summary>
    public static class TtmlParser
    {
        private static readonly XNamespace NsTtml = "http://www.w3.org/ns/ttml";
        private static readonly XNamespace NsTtm = "http://www.w3.org/ns/ttml#metadata";
        private static readonly XNamespace NsItunes = "http://music.apple.com/lyric-ttml-internal";
        private static readonly XNamespace NsXml = "http://www.w3.org/XML/1998/namespace";

        private sealed class Agent
        {
            public string Id { get; init; } = "";
            public string Type { get; init; } = ""; // person/group/other/...
        }

        public static LyricsData Parse(string ttml)
        {
            var data = new LyricsData
            {
                TrackMetadata = new TrackMetadata(),
                File = new()
                {
                    Type = LyricsTypes.Ttml,
                    SyncTypes = SyncTypes.SyllableSynced,
                    AdditionalInfo = new GeneralAdditionalInfo { Attributes = new List<KeyValuePair<string, string>>() }
                },
                Lines = new List<ILineInfo>()
            };

            if (string.IsNullOrWhiteSpace(ttml))
                return data;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(ttml, LoadOptions.PreserveWhitespace);
            }
            catch
            {
                return data;
            }

            ParseITunesMetadata(doc, data);
            var translations = ParseTranslations(doc);
            var agents = ParseAgents(doc);

            var pNodes = doc.Descendants(NsTtml + "p").ToList();
            bool anyLineSynced = false;
            bool anySyllableSynced = false;

            foreach (var p in pNodes)
            {
                var key = (string?)p.Attribute(NsItunes + "key") ?? (string?)p.Attribute("key");
                var agentId = (string?)p.Attribute(NsTtm + "agent");

                var mainSyllables = new List<SyllableInfo>();
                var bgSyllables = new List<SyllableInfo>();
                CollectSyllablesFromNodes(p.Nodes(), mainSyllables, bgSyllables, isBackgroundContext: false);

                ILineInfo? line = null;

                if (mainSyllables.Count > 0)
                {
                    anySyllableSynced = true;
                    line = new SyllableLineInfo(mainSyllables);
                }
                else
                {
                    var begin = ParseTimeMs((string?)p.Attribute("begin"));
                    var end = ParseTimeMs((string?)p.Attribute("end"));
                    var text = NormalizeText(p.Value).Trim();

                    if (!string.IsNullOrWhiteSpace(text) && begin.HasValue)
                    {
                        anyLineSynced = true;
                        line = new LineInfo(text, begin.Value, end);
                    }
                }

                if (line == null)
                    continue;

                // Alignment
                var align = GetLyricsAlignmentFromAgent(agentId, agents);
                SetAlignment(line, align);

                // Background -> SubLine
                if (bgSyllables.Count > 0)
                {
                    NormalizeBracketInnerSpacingForBgLyrics(bgSyllables);

                    var subLine = (ILineInfo)new SyllableLineInfo(bgSyllables);
                    SetAlignment(subLine, align);
                    SetSubLine(line, subLine);
                }

                // Translations
                if (!string.IsNullOrEmpty(key) && translations.TryGetValue(key, out var tmap))
                {
                    // replacement: replace MAIN line only
                    var replacement = tmap
                        .Where(kv => kv.Key.type.Equals("replacement", StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Value)
                        .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

                    if (!string.IsNullOrWhiteSpace(replacement))
                    {
                        line = ApplyReplacementMainOnly(line, replacement!);
                        SetAlignment(line, align);
                    }

                    // subtitle translations
                    var subtitles = tmap
                        .Where(kv => kv.Key.type.Equals("subtitle", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (subtitles.Count > 0)
                    {
                        line = ApplySubtitleTranslations(line, subtitles);
                        SetAlignment(line, align);
                    }
                }

                data.Lines!.Add(line);
            }

            if (anyLineSynced && anySyllableSynced) data.File!.SyncTypes = SyncTypes.MixedSynced;
            else if (anyLineSynced) data.File!.SyncTypes = SyncTypes.LineSynced;
            else if (anySyllableSynced) data.File!.SyncTypes = SyncTypes.SyllableSynced;
            else data.File!.SyncTypes = SyncTypes.Unknown;

            return data;
        }

        // =========================
        // Safe setters (ILineInfo has readonly properties)
        // =========================
        private static void SetAlignment(ILineInfo line, LyricsAlignment alignment)
        {
            if (line is LineInfo li) li.LyricsAlignment = alignment;
            else if (line is SyllableLineInfo sl) sl.LyricsAlignment = alignment;
        }

        private static void SetSubLine(ILineInfo line, ILineInfo? subLine)
        {
            if (line is LineInfo li) li.SubLine = subLine;
            else if (line is SyllableLineInfo sl) sl.SubLine = subLine;
        }

        // =========================
        // Duet alignment
        // =========================
        private static List<Agent> ParseAgents(XDocument doc)
        {
            var agents = new List<Agent>();
            foreach (var a in doc.Descendants(NsTtm + "agent"))
            {
                var id = (string?)a.Attribute(NsXml + "id");
                if (string.IsNullOrWhiteSpace(id)) continue;

                var type = ((string?)a.Attribute("type") ?? string.Empty).Trim();
                agents.Add(new Agent { Id = id.Trim(), Type = type });
            }
            return agents;
        }

        private static LyricsAlignment GetLyricsAlignmentFromAgent(string? agentId, List<Agent> agents)
        {
            if (agents == null || agents.Count == 0) return LyricsAlignment.Unspecified;
            if (agents.Count == 1) return LyricsAlignment.Left;
            if (string.IsNullOrWhiteSpace(agentId)) return LyricsAlignment.Unspecified;

            List<Agent> persons = new();
            List<Agent> groups = new();
            List<Agent> others = new();
            List<Agent> rest = new();

            foreach (var agent in agents)
            {
                if (agent.Type == "person") persons.Add(agent);
                else if (agent.Type == "group") groups.Add(agent);
                else if (agent.Type == "other") others.Add(agent);
                else rest.Add(agent);
            }

            var hit = agents.Find(a => a.Id == agentId);
            if (hit == null) return LyricsAlignment.Unspecified;

            bool left;
            if (hit.Type == "person") left = persons.IndexOf(hit) % 2 == 0;
            else if (hit.Type == "group") left = groups.IndexOf(hit) % 2 == 0;
            else if (hit.Type == "other") left = others.IndexOf(hit) % 2 == 0;
            else left = rest.IndexOf(hit) % 2 == 0;

            return left ? LyricsAlignment.Left : LyricsAlignment.Right;
        }

        // =========================
        // iTunes metadata + translations
        // =========================
        private static void ParseITunesMetadata(XDocument doc, LyricsData data)
        {
            var meta = doc.Descendants(NsItunes + "iTunesMetadata").FirstOrDefault();
            if (meta == null) return;

            var leadingSilence = (string?)meta.Attribute("leadingSilence");
            if (!string.IsNullOrWhiteSpace(leadingSilence))
            {
                (data.File!.AdditionalInfo as GeneralAdditionalInfo)!.Attributes!
                    .Add(new KeyValuePair<string, string>("leadingSilence", leadingSilence));
            }

            var writers = meta.Descendants(NsItunes + "songwriter")
                .Select(x => (x.Value ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (writers.Count > 0)
                data.Writers = writers;
        }

        private static Dictionary<string, Dictionary<(string type, string lang), string>> ParseTranslations(XDocument doc)
        {
            var result = new Dictionary<string, Dictionary<(string type, string lang), string>>(StringComparer.Ordinal);

            foreach (var translation in doc.Descendants(NsItunes + "translation"))
            {
                var type = ((string?)translation.Attribute("type") ?? string.Empty).Trim();
                var lang = ((string?)translation.Attribute(NsXml + "lang") ?? string.Empty).Trim();

                foreach (var textNode in translation.Elements(NsItunes + "text"))
                {
                    var key = ((string?)textNode.Attribute("for") ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(key)) continue;

                    var value = NormalizeText(textNode.Value).Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    if (!result.TryGetValue(key, out var map))
                    {
                        map = new Dictionary<(string type, string lang), string>();
                        result[key] = map;
                    }

                    map[(type, lang)] = value;
                }
            }

            return result;
        }

        // =========================
        // Syllable collection (main + background)
        // =========================
        private static void CollectSyllablesFromNodes(IEnumerable<XNode> nodes, List<SyllableInfo> main, List<SyllableInfo> bg, bool isBackgroundContext)
        {
            foreach (var node in nodes)
            {
                if (node is XText xt)
                {
                    AppendToPrevious(xt.Value, isBackgroundContext ? bg : main);
                    continue;
                }

                if (node is XElement el)
                {
                    if (el.Name == NsTtml + "span")
                    {
                        var role = (string?)el.Attribute(NsTtm + "role") ?? (string?)el.Attribute("role");
                        var isBg = isBackgroundContext || string.Equals(role, "x-bg", StringComparison.OrdinalIgnoreCase);

                        var beginAttr = (string?)el.Attribute("begin");
                        if (!string.IsNullOrWhiteSpace(beginAttr))
                        {
                            var beginMs = ParseTimeMs(beginAttr);
                            var endMs = ParseTimeMs((string?)el.Attribute("end")) ?? beginMs;

                            if (beginMs.HasValue)
                            {
                                var raw = NormalizeText(el.Value);

                                // Spaces belong to previous token
                                raw = MoveLeadingSpacesToPrevious(raw, isBg ? bg : main);

                                if (raw.Length > 0)
                                {
                                    (isBg ? bg : main).Add(new SyllableInfo(raw, beginMs.Value, endMs!.Value));
                                }
                            }
                        }
                        else
                        {
                            // container span
                            CollectSyllablesFromNodes(el.Nodes(), main, bg, isBg);
                        }
                    }
                    else
                    {
                        CollectSyllablesFromNodes(el.Nodes(), main, bg, isBackgroundContext);
                    }
                }
            }
        }

        private static void AppendToPrevious(string text, List<SyllableInfo> list)
        {
            if (string.IsNullOrEmpty(text) || list.Count == 0) return;
            text = NormalizeText(text);
            if (text.Length == 0) return;

            // whitespace/punct belongs to previous syllable
            list[^1].Text += text;
        }

        private static string MoveLeadingSpacesToPrevious(string text, List<SyllableInfo> list)
        {
            if (string.IsNullOrEmpty(text) || list.Count == 0) return text;

            int i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i == 0) return text;

            list[^1].Text += text[..i];
            return text[i..];
        }

        private static void NormalizeBracketInnerSpacingForBgLyrics(List<SyllableInfo> syllables)
        {
            if (syllables == null || syllables.Count == 0)
                return;

            syllables[0].Text = Regex.Replace(
                syllables[0].Text,
                @"\s+(\(|（)",
                "$1"
            );

            syllables[^1].Text = Regex.Replace(
                syllables[^1].Text,
                @"(\)|）)\s+",
                "$1"
            );
        }

        // =========================
        // Replacement / subtitle translations
        // =========================
        private static ILineInfo ApplyReplacementMainOnly(ILineInfo line, string replacement)
        {
            // local helpers to keep this function self-contained
            static string NormalizeText(string s) => (s ?? string.Empty).Replace("\r", "").Replace("\n", "");

            static string NormalizeSpaces(string s)
                => Regex.Replace((s ?? string.Empty).Trim(), @"\s+", " ");

            // Extract ONLY the first bracket segment (supports () and （）).
            // Returns:
            // - mainText: original with that first bracket segment removed
            // - bracketSegmentWithBrackets: the matched "(...)" or "（...）" INCLUDING brackets (or null if none)
            static (string mainText, string? bracketSegmentWithBrackets) SplitFirstBracketSegmentKeepBrackets(string text)
            {
                text = NormalizeText(text);

                var m = Regex.Match(text, @"\(([^)]*)\)|（([^）]*)）");
                if (!m.Success)
                    return (NormalizeSpaces(text), null);

                // matched string includes brackets already
                var bracketSeg = m.Value;

                // remove only this first match
                var main = text.Remove(m.Index, m.Length);
                main = NormalizeSpaces(main);

                return (main, bracketSeg);
            }

            // For background: remove spaces OUTSIDE brackets (left bracket preceding spaces / right bracket trailing spaces),
            // keep brackets themselves and inner content unchanged.
            static string NormalizeBracketOuterSpaces(string s)
            {
                s = NormalizeText(s);

                // Remove spaces before '(' or '（'
                s = Regex.Replace(s, @"\s+(\(|（)", "$1");
                // Remove spaces after ')' or '）'
                s = Regex.Replace(s, @"(\)|）)\s+", "$1");

                return s;
            }

            // Replace a syllable line's text with best-effort length mapping; fallback to single syllable
            static SyllableLineInfo ReplaceSyllableLineText(SyllableLineInfo sLine, string newText)
            {
                newText = NormalizeText(newText);

                var sylls = sLine.Syllables.ToList();
                var lens = sylls.Select(s => s.Text?.Length ?? 0).ToList();
                var totalLen = lens.Sum();

                if (totalLen == newText.Length && totalLen > 0)
                {
                    int idx = 0;
                    var newSylls = new List<ISyllableInfo>(sylls.Count);

                    for (int i = 0; i < sylls.Count; i++)
                    {
                        int len = lens[i];
                        var chunk = len > 0 ? newText.Substring(idx, len) : string.Empty;
                        idx += len;

                        newSylls.Add(new SyllableInfo(chunk, sylls[i].StartTime, sylls[i].EndTime));
                    }

                    return new SyllableLineInfo(newSylls);
                }

                // fallback: collapse to one syllable spanning the original line time range
                var start = sLine.StartTime ?? 0;
                var end = sLine.EndTime ?? start;
                return new SyllableLineInfo(new[] { new SyllableInfo(newText, start, end) });
            }

            // Replace a line's text (mainly for LineInfo)
            static LineInfo ReplaceLineText(LineInfo li, string newText)
            {
                li.Text = NormalizeText(newText);
                return li;
            }

            replacement = NormalizeText(replacement);

            // Preserve existing subline reference (may be null)
            var existingSub = line.SubLine;

            // If this line has background vocals (SubLine exists), split replacement by FIRST bracket segment:
            // - main replacement removes the bracket segment
            // - bg replacement uses the bracket segment (keeps brackets)
            string mainReplacement = replacement;
            string? bgReplacement = null;

            if (existingSub != null)
            {
                var (mainText, bracketSegmentWithBrackets) = SplitFirstBracketSegmentKeepBrackets(replacement);
                mainReplacement = mainText;
                bgReplacement = bracketSegmentWithBrackets;

                if (!string.IsNullOrWhiteSpace(bgReplacement))
                    bgReplacement = NormalizeBracketOuterSpaces(bgReplacement!);
            }

            // ---- Apply to MAIN line ----
            ILineInfo newMain;

            if (line is SyllableLineInfo sMain)
            {
                var replaced = ReplaceSyllableLineText(sMain, mainReplacement);
                newMain = replaced;
            }
            else if (line is LineInfo liMain)
            {
                newMain = ReplaceLineText(liMain, mainReplacement);
            }
            else
            {
                // Unknown type: do nothing
                newMain = line;
            }

            // ---- Apply to SUBLINE (background) if we extracted a bracket segment and subline exists ----
            if (existingSub != null && !string.IsNullOrWhiteSpace(bgReplacement))
            {
                ILineInfo newSub = existingSub;

                if (existingSub is SyllableLineInfo sSub)
                {
                    newSub = ReplaceSyllableLineText(sSub, bgReplacement!);
                }
                else if (existingSub is LineInfo liSub)
                {
                    newSub = ReplaceLineText(liSub, bgReplacement!);
                }

                // Attach subline back (ILineInfo.SubLine is readonly, but LineInfo/SyllableLineInfo aren't)
                if (newMain is LineInfo li) li.SubLine = newSub;
                else if (newMain is SyllableLineInfo sl) sl.SubLine = newSub;
                // else: cannot attach
            }
            else
            {
                // Keep existing subline if any
                if (existingSub != null)
                {
                    if (newMain is LineInfo li) li.SubLine = existingSub;
                    else if (newMain is SyllableLineInfo sl) sl.SubLine = existingSub;
                }
            }

            return newMain;
        }

        private static ILineInfo ApplySubtitleTranslations(
            ILineInfo line,
            List<KeyValuePair<(string type, string lang), string>> subtitles)
        {
            var align = line.LyricsAlignment;
            var subLine = line.SubLine; // may be null

            // Build lang->value map (dedupe)
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in subtitles)
            {
                var langKey = NormalizeLangKey(kv.Key.lang);
                if (!dict.ContainsKey(langKey))
                    dict[langKey] = kv.Value;
            }

            // If has bg line: split translations into main/bg parts.
            // - bg part comes from (...) in translation, stored WITHOUT brackets.
            // - main part removes (...) segments.
            Dictionary<string, string>? bgDict = null;
            if (subLine != null)
            {
                bgDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var k in dict.Keys.ToList())
                {
                    var (mainText, bgText) = SplitSubtitleByParentheses(dict[k]);
                    dict[k] = mainText;

                    if (!string.IsNullOrWhiteSpace(bgText))
                        bgDict[k] = bgText!;
                }
            }

            // Apply to MAIN line
            ILineInfo mainOut;
            if (line is SyllableLineInfo sLine)
            {
                var full = line as FullSyllableLineInfo ?? new FullSyllableLineInfo(sLine);
                foreach (var kv in dict)
                {
                    if (!full.Translations.ContainsKey(kv.Key))
                        full.Translations[kv.Key] = kv.Value;
                }
                mainOut = full;
            }
            else
            {
                if (line is not LineInfo li) return line;

                var full = line as FullLineInfo ?? new FullLineInfo(li);
                foreach (var kv in dict)
                {
                    if (!full.Translations.ContainsKey(kv.Key))
                        full.Translations[kv.Key] = kv.Value;
                }
                mainOut = full;
            }

            // Apply to SUBLINE (background translation) if extracted
            if (subLine != null && bgDict != null && bgDict.Count > 0)
            {
                ILineInfo subOut = subLine;

                if (subLine is SyllableLineInfo subSyll)
                {
                    var subFull = subLine as FullSyllableLineInfo ?? new FullSyllableLineInfo(subSyll);
                    foreach (var kv in bgDict)
                    {
                        if (!subFull.Translations.ContainsKey(kv.Key))
                            subFull.Translations[kv.Key] = kv.Value;
                    }
                    subOut = subFull;
                }
                else if (subLine is LineInfo subLi)
                {
                    var subFull = subLine as FullLineInfo ?? new FullLineInfo(subLi);
                    foreach (var kv in bgDict)
                    {
                        if (!subFull.Translations.ContainsKey(kv.Key))
                            subFull.Translations[kv.Key] = kv.Value;
                    }
                    subOut = subFull;
                }

                // reattach subline to main
                SetSubLine(mainOut, subOut);
            }
            else
            {
                // keep existing subline
                SetSubLine(mainOut, subLine);
            }

            // keep alignment
            SetAlignment(mainOut, align);
            return mainOut;
        }

        private static (string main, string? bg) SplitSubtitleByParentheses(string value)
        {
            value = NormalizeText(value);

            var m = Regex.Match(value, @"\(([^)]*)\)|（([^）]*)）");
            if (!m.Success)
            {
                return (NormalizeSpaces(value), null);
            }

            string inner = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            inner = NormalizeSpaces(inner);

            var main = value.Remove(m.Index, m.Length);
            main = NormalizeSpaces(main);

            return (main, string.IsNullOrWhiteSpace(inner) ? null : inner);
        }

        private static string NormalizeSpaces(string s)
        {
            s = s.Trim();
            // collapse whitespace runs into single spaces
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static string NormalizeLangKey(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return "und";
            lang = lang.Trim();
            if (lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh";
            return lang;
        }

        // =========================
        // Time parsing
        // =========================
        private static string NormalizeText(string text) => text.Replace("\r", "").Replace("\n", "");

        private static int? ParseTimeMs(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();

            if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                value = value[..^1];

            try
            {
                if (value.Contains(':'))
                {
                    var parts = value.Split(':');
                    double seconds;

                    if (parts.Length == 2)
                    {
                        var minutes = double.Parse(parts[0], CultureInfo.InvariantCulture);
                        var sec = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        seconds = minutes * 60d + sec;
                    }
                    else if (parts.Length == 3)
                    {
                        var hours = double.Parse(parts[0], CultureInfo.InvariantCulture);
                        var minutes = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        var sec = double.Parse(parts[2], CultureInfo.InvariantCulture);
                        seconds = hours * 3600d + minutes * 60d + sec;
                    }
                    else
                    {
                        seconds = double.Parse(value.Replace(':', '.'), CultureInfo.InvariantCulture);
                    }

                    return (int)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);
                }

                var s2 = double.Parse(value, CultureInfo.InvariantCulture);
                return (int)Math.Round(s2 * 1000d, MidpointRounding.AwayFromZero);
            }
            catch
            {
                return null;
            }
        }
    }
}
