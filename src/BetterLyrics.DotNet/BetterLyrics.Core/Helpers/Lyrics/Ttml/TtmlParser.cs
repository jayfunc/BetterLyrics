using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BetterLyrics.Core.Helpers.Lyrics.Ttml;

internal static partial class TtmlParser
{
    private static readonly XNamespace Ttm = "http://www.w3.org/ns/ttml#metadata";
    private static readonly XNamespace Tts = "http://www.w3.org/ns/ttml#styling";
    private static readonly XNamespace Amll = "http://www.example.com/ns/amll";
    private static readonly XNamespace Itunes = "http://music.apple.com/lyric-ttml-internal";
    private static readonly XNamespace LegacyItunes = "http://itunes.apple.com/lyric-ttml-extensions";

    private static string? AppleAttribute(XElement element, string name) =>
        (string?)element.Attribute(Itunes + name) ?? (string?)element.Attribute(LegacyItunes + name);

    internal static XElement Load(string content)
    {
        using var input = new StringReader(content);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        var root = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo).Root;
        if (root?.Name.LocalName != "tt") throw new FormatException("Expected a TTML tt root element.");
        return root;
    }

    public static TtmlDocument Parse(string content)
    {
        var root = Load(content);
        var result = new TtmlDocument { Metadata = ParseMetadata(root) };
        var auxiliary = ReadHeadSubLyrics(root);
        uint block = 0;
        foreach (var body in root.Elements().Where(e => e.Name.LocalName == "body"))
        foreach (var div in body.Descendants().Where(e => e.Name.LocalName == "div" &&
            !e.Ancestors().TakeWhile(a => a != body).Any(a => a.Name.LocalName == "div")))
        {
            block++;
            foreach (var p in div.Descendants().Where(e => e.Name.LocalName == "p" &&
                !e.Ancestors().TakeWhile(a => a != div).Any(a => a.Name.LocalName == "p")))
            {
                var line = new TtmlLine
                {
                    StartTime = RequiredTime(p, "begin"), EndTime = RequiredTime(p, "end"),
                    Id = AppleAttribute(p, "key"), AgentId = (string?)p.Attribute(Ttm + "agent"),
                    SongPart = AppleAttribute(div, "songPart") ?? AppleAttribute(div, "song-part"),
                    BlockIndex = block
                };
                if (line.Id != null && auxiliary.Remove(line.Id, out var entries))
                    foreach (var entry in entries)
                    {
                        var target = entry.Background ? line.BackgroundVocal ??= new() : line;
                        AddSubLyric(target, entry.Content, entry.Roman);
                    }
                ReadVocal(p, line, line, false);
                NormalizeVocal(line, false);
                if (line.BackgroundVocal != null) NormalizeVocal(line.BackgroundVocal, true);
                result.Lines.Add(line);
            }
        }
        return result;
    }

    private static void ReadVocal(XElement element, TtmlVocal vocal, TtmlLine line, bool background)
    {
        var raw = new StringBuilder();
        foreach (var node in ContentNodes(element))
        {
            if (node is XText text)
            {
                raw.Append(text.Value);
                MarkSpace(vocal.Words, text.Value);
            }
            else if (node is XElement span && span.Name.LocalName == "span")
            {
                var role = (string?)span.Attribute(Ttm + "role");
                if (role is "x-translation" or "x-roman")
                {
                    if (!string.IsNullOrWhiteSpace(span.Value))
                        AddSubLyric(vocal, new TtmlSubLyric
                        {
                            Language = (string?)span.Attribute(XNamespace.Xml + "lang"),
                            Text = span.Value.Trim()
                        }, role == "x-roman");
                }
                else if (role == "x-bg" && !background)
                {
                    var bg = line.BackgroundVocal ??= new();
                    ReadVocal(span, bg, line, true);
                    bg.StartTime = OptionalTime(span, "begin") ?? bg.Words?.FirstOrDefault()?.StartTime ?? 0;
                    bg.EndTime = OptionalTime(span, "end") ?? bg.Words?.LastOrDefault()?.EndTime ?? 0;
                }
                else (vocal.Words ??= []).Add(ReadWord(span, true));
            }
        }
        vocal.Text = raw.ToString();
    }

    // Walk unknown wrappers without recursive calls; span subtrees are consumed by their own parser.
    private static IEnumerable<XNode> ContentNodes(XElement element)
    {
        var node = element.FirstNode;
        while (node != null)
        {
            if (node is XElement child && child.Name.LocalName != "span" && child.FirstNode != null)
            {
                node = child.FirstNode;
                continue;
            }
            yield return node;
            while (node.NextNode == null && node.Parent != element) node = node.Parent!;
            node = node.NextNode;
        }
    }

    private static TtmlWord ReadWord(XElement span, bool allowRuby)
    {
        var word = new TtmlWord
        {
            Obscene = span.Attribute(Amll + "obscene") is { } obscene ? obscene.Value == "true" : null,
            EmptyBeat = uint.TryParse((string?)span.Attribute(Amll + "empty-beat"),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var beat) ? beat : null
        };
        if (allowRuby && (string?)span.Attribute(Tts + "ruby") == "container")
        {
            word.Text = span.Elements().LastOrDefault(e => (string?)e.Attribute(Tts + "ruby") == "base")?.Value ?? "";
            word.Ruby = [];
            var container = span.Elements().LastOrDefault(e => (string?)e.Attribute(Tts + "ruby") == "textContainer");
            if (container != null)
                foreach (var ruby in container.Descendants().Where(e => (string?)e.Attribute(Tts + "ruby") == "text"))
                    word.Ruby.Add(new TtmlRuby
                    {
                        Text = ruby.Value, StartTime = RequiredTime(ruby, "begin"), EndTime = RequiredTime(ruby, "end")
                    });
            word.StartTime = word.Ruby.FirstOrDefault()?.StartTime ?? 0;
            word.EndTime = word.Ruby.LastOrDefault()?.EndTime ?? 0;
        }
        else
        {
            word.Text = span.Value;
            word.StartTime = RequiredTime(span, "begin");
            word.EndTime = RequiredTime(span, "end");
        }
        return word;
    }

    private static void AddSubLyric(TtmlVocal target, TtmlSubLyric content, bool roman)
    {
        if (roman) (target.Romanizations ??= []).Add(content);
        else (target.Translations ??= []).Add(content);
    }

    private static void MarkSpace(List<TtmlWord>? words, string text)
    {
        if (text.Length > 0 && !text.Contains('\n') && words is { Count: > 0 }) words[^1].EndsWithSpace = true;
    }

    private static void NormalizeVocal(TtmlVocal vocal, bool background)
    {
        if (vocal.Words is { Count: > 0 }) vocal.Words[^1].EndsWithSpace = null;
        Normalize(vocal, background, false);
        foreach (var t in vocal.Translations ?? []) Normalize(t, background, false);
        foreach (var r in vocal.Romanizations ?? []) Normalize(r, background, true);
    }

    private static string StripParens(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length >= 2 && trimmed[0] is '(' or '（' && trimmed[^1] is ')' or '）'
            ? trimmed[1..^1].Trim() : text;
    }

    private static void Normalize(TtmlContent content, bool stripParens, bool spaceJoined)
    {
        if (content.Words is not { } words)
        {
            var text = stripParens ? StripParens(content.Text) : content.Text;
            content.Text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return;
        }
        if (stripParens && words.Count > 0)
        {
            if (words.Count == 1) words[0].Text = StripParens(words[0].Text);
            else if (words[0].Text.TrimStart().StartsWith('(') || words[0].Text.TrimStart().StartsWith('（'))
            {
                if (words[^1].Text.TrimEnd().EndsWith(')') || words[^1].Text.TrimEnd().EndsWith('）'))
                {
                    words[0].Text = words[0].Text.Remove(words[0].Text.IndexOfAny(['(', '（']), 1);
                    words[^1].Text = words[^1].Text.Remove(words[^1].Text.LastIndexOfAny([')', '）']), 1);
                }
            }
        }
        for (var i = 0; i < words.Count; i++)
        {
            var text = words[i].Text;
            if (text.Length == 0) continue;
            if (text.TrimStart().Length < text.Length && i > 0) words[i - 1].EndsWithSpace = true;
            if (text.Trim().Length > 0 && text.TrimEnd().Length < text.Length) words[i].EndsWithSpace = true;
            words[i].Text = text.Trim();
        }
        words.RemoveAll(w => w.Text.Length == 0);
        content.Text = string.Concat(words.Select(w => w.Text + (spaceJoined || w.EndsWithSpace == true ? " " : ""))).TrimEnd(' ');
    }

    internal static uint ParseTimestamp(string value)
    {
        ulong total = 0, current = 0;
        var colons = 0;
        var fraction = 0u;
        var digits = 0;
        var fractional = false;
        foreach (var c in value)
        {
            if (c is >= '0' and <= '9')
            {
                if (fractional)
                {
                    if (digits < 3) { fraction = fraction * 10 + (uint)(c - '0'); digits++; }
                }
                else
                {
                    current = current * 10 + (uint)(c - '0');
                    if (current > uint.MaxValue) throw new FormatException($"Invalid TTML timestamp: {value}");
                }
            }
            else if (c == ':' && !fractional && ++colons <= 2)
            {
                total = (total + current) * 60;
                current = 0;
                if (total > uint.MaxValue) throw new FormatException($"Invalid TTML timestamp: {value}");
            }
            else if (c == '.' && !fractional) fractional = true;
            else throw new FormatException($"Invalid TTML timestamp: {value}");
        }
        if (digits == 1) fraction *= 100;
        else if (digits == 2) fraction *= 10;
        total = (total + current) * 1000 + fraction;
        if (total > uint.MaxValue) throw new FormatException($"Invalid TTML timestamp: {value}");
        return (uint)total;
    }

    private static uint RequiredTime(XElement element, string name) =>
        OptionalTime(element, name) ?? throw Error(element, $"Missing {name} attribute");

    private static uint? OptionalTime(XElement element, string name)
    {
        if (element.Attribute(name) is not { } attribute) return null;
        try { return ParseTimestamp(attribute.Value); }
        catch (FormatException ex) { throw Error(element, $"Invalid {name}: {attribute.Value}", ex); }
    }

    private static FormatException Error(XElement element, string message, Exception? inner = null)
    {
        var location = (IXmlLineInfo)element;
        var line = element.AncestorsAndSelf().FirstOrDefault(e => e.Name.LocalName == "p");
        return new FormatException($"{message} on <{element.Name.LocalName}> at {location.LineNumber}:{location.LinePosition}, lyric { (line == null ? "" : AppleAttribute(line, "key")) }.", inner);
    }
}
