using System.Text;
using System.Xml.Linq;

namespace BetterLyrics.Core.Helpers.Lyrics.Ttml;

internal static partial class TtmlParser
{
    internal static TtmlMetadata ParseMetadata(XElement root)
    {
        var metadata = new TtmlMetadata
        {
            Language = (string?)root.Attribute(XNamespace.Xml + "lang"),
            TimingMode = AppleAttribute(root, "timing")
        };
        var seenAudio = false;
        foreach (var e in root.Elements().Where(e => e.Name.LocalName == "head").SelectMany(e => e.Descendants()))
        {
            if (e.Name == Ttm + "title" && e.Value.Trim() is { Length: > 0 } title)
                (metadata.Title ??= []).Add(title);
            else if (e.Name == Ttm + "agent")
            {
                var id = (string?)e.Attribute(XNamespace.Xml + "id") ?? throw Error(e, "Missing xml:id attribute");
                (metadata.Agents ??= [])[id] = new TtmlAgent
                {
                    Id = id, Type = (string?)e.Attribute("type"),
                    Name = e.Descendants(Ttm + "name").LastOrDefault()?.Value
                };
            }
            else if (e.Name == Amll + "meta" && (string?)e.Attribute("key") is { } key &&
                ((string?)e.Attribute("value"))?.Trim() is { Length: > 0 } value)
            {
                switch (key)
                {
                    case "musicName": (metadata.Title ??= []).Add(value); break;
                    case "artists": (metadata.Artist ??= []).Add(value); break;
                    case "album": (metadata.Album ??= []).Add(value); break;
                    case "isrc": (metadata.Isrc ??= []).Add(value); break;
                    case "ttmlAuthorGithub": (metadata.AuthorIds ??= []).Add(value); break;
                    case "ttmlAuthorGithubLogin": (metadata.AuthorNames ??= []).Add(value); break;
                    case "ncmMusicId": case "qqMusicId": case "spotifyId": case "appleMusicId":
                        AddProperty(metadata.PlatformIds ??= [], key, value); break;
                    default: AddProperty(metadata.RawProperties ??= [], key, value); break;
                }
            }
            else if (e.Name.LocalName == "songwriter" && e.Value.Trim() is { Length: > 0 } writer)
                (metadata.Songwriters ??= []).Add(writer);
            else if (e.Name.LocalName == "iTunesMetadata")
            {
                var silence = DecimalSeconds((string?)e.Attribute("leadingSilence"));
                if (silence is >= 0 and <= uint.MaxValue) (metadata.Audio ??= new()).LeadingSilence = (uint)silence;
            }
            else if (e.Name.LocalName == "audio" && e.Parent?.Name.LocalName == "iTunesMetadata" && !seenAudio)
            {
                seenAudio = true;
                var offset = DecimalSeconds((string?)e.Attribute("lyricOffset"));
                var role = ((string?)e.Attribute("role"))?.Trim();
                if (offset is >= int.MinValue and <= int.MaxValue) (metadata.Audio ??= new()).LyricOffset = (int)offset;
                if (!string.IsNullOrEmpty(role)) (metadata.Audio ??= new()).Role = role;
            }
        }
        foreach (var body in root.Elements().Where(e => e.Name.LocalName == "body"))
            if ((string?)body.Attribute("dur") is { } duration)
                try { (metadata.Audio ??= new()).Duration = ParseTimestamp(duration.Trim()); }
                catch (FormatException) { }
        if (metadata.Audio is { Duration: null, LeadingSilence: null, LyricOffset: null, Role: null }) metadata.Audio = null;
        return metadata;
    }

    private static void AddProperty(Dictionary<string, List<string>> properties, string key, string value)
    {
        if (!properties.TryGetValue(key, out var values)) properties[key] = values = [];
        values.Add(value);
    }

    private static long? DecimalSeconds(string? value)
    {
        if (value == null) return null;
        value = value.Trim();
        var negative = value.StartsWith('-');
        if (value.StartsWith('-') || value.StartsWith('+')) value = value[1..];
        if (value.Contains(':') || !value.Any(c => c is >= '0' and <= '9')) return null;
        try { return (negative ? -1L : 1L) * ParseTimestamp(value); }
        catch (FormatException) { return null; }
    }

    private sealed record HeadSubLyric(TtmlSubLyric Content, bool Roman, bool Background);

    private static Dictionary<string, List<HeadSubLyric>> ReadHeadSubLyrics(XElement root)
    {
        Dictionary<string, List<HeadSubLyric>> result = [];
        foreach (var group in root.Elements().Where(e => e.Name.LocalName == "head")
            .SelectMany(e => e.Descendants()).Where(e => e.Name.LocalName is "translation" or "transliteration"))
        foreach (var text in group.Descendants().Where(e => e.Name.LocalName == "text"))
        {
            var id = (string?)text.Attribute("for") ?? throw Error(text, "Missing for attribute");
            if (!result.TryGetValue(id, out var entries)) result[id] = entries = [];
            var main = new TtmlSubLyric();
            var bg = new TtmlSubLyric();
            ReadSubContent(text, main, bg, false);
            foreach (var (content, background) in new[] { (main, false), (bg, true) })
            {
                if (content.Words == null && string.IsNullOrWhiteSpace(content.Text)) continue;
                content.Language = (string?)group.Attribute(XNamespace.Xml + "lang");
                content.AutomaticallyCreated = (string?)group.Attribute("automaticallyCreated") is "true" or "1" ? true : null;
                if (content.Words is { Count: > 0 }) content.Words[^1].EndsWithSpace = null;
                Normalize(content, false, false);
                entries.Add(new(content, group.Name.LocalName == "transliteration", background));
            }
        }
        return result;
    }

    private static void ReadSubContent(XElement element, TtmlSubLyric main, TtmlSubLyric bg, bool background)
    {
        var target = background ? bg : main;
        var raw = new StringBuilder(target.Text);
        foreach (var node in element.Nodes())
        {
            if (node is XText text)
            {
                raw.Append(text.Value);
                MarkSpace(target.Words, text.Value);
            }
            else if (node is XElement span)
            {
                if (span.Name.LocalName == "span" && (string?)span.Attribute(Ttm + "role") == "x-bg")
                    ReadSubContent(span, main, bg, true);
                else if (span.Name.LocalName == "span") (target.Words ??= []).Add(ReadWord(span, false));
                else
                {
                    target.Text = raw.ToString();
                    ReadSubContent(span, main, bg, background);
                    raw.Clear().Append(target.Text);
                }
            }
        }
        target.Text = raw.ToString();
    }
}
