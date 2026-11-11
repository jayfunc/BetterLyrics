namespace BetterLyrics.Core.Helpers.Lyrics.Ttml;

// C# port of ttml-processor-rs; see README.md and LICENSE in this directory.
internal sealed class TtmlDocument
{
    public TtmlMetadata Metadata { get; set; } = new();
    public List<TtmlLine> Lines { get; set; } = [];
}

internal sealed class TtmlMetadata
{
    public string? Language { get; set; }
    public string? TimingMode { get; set; }
    public TtmlAudio? Audio { get; set; }
    public List<string>? Songwriters { get; set; }
    public List<string>? Title { get; set; }
    public List<string>? Artist { get; set; }
    public List<string>? Album { get; set; }
    public List<string>? Isrc { get; set; }
    public List<string>? AuthorIds { get; set; }
    public List<string>? AuthorNames { get; set; }
    public Dictionary<string, TtmlAgent>? Agents { get; set; }
    public Dictionary<string, List<string>>? PlatformIds { get; set; }
    public Dictionary<string, List<string>>? RawProperties { get; set; }
}

internal sealed class TtmlAudio
{
    public uint? Duration { get; set; }
    public int? LyricOffset { get; set; }
    public string? Role { get; set; }
    public uint? LeadingSilence { get; set; }
}

internal sealed class TtmlAgent
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Type { get; set; }
}

internal class TtmlContent
{
    public string Text { get; set; } = "";
    public List<TtmlWord>? Words { get; set; }
}

internal sealed class TtmlSubLyric : TtmlContent
{
    public string? Language { get; set; }
    public bool? AutomaticallyCreated { get; set; }
}

internal class TtmlVocal : TtmlContent
{
    public uint StartTime { get; set; }
    public uint EndTime { get; set; }
    public List<TtmlSubLyric>? Translations { get; set; }
    public List<TtmlSubLyric>? Romanizations { get; set; }
}

internal sealed class TtmlLine : TtmlVocal
{
    public TtmlVocal? BackgroundVocal { get; set; }
    public string? Id { get; set; }
    public string? AgentId { get; set; }
    public string? SongPart { get; set; }
    public uint? BlockIndex { get; set; }
}

internal class TtmlRuby
{
    public string Text { get; set; } = "";
    public uint StartTime { get; set; }
    public uint EndTime { get; set; }
}

internal sealed class TtmlWord : TtmlRuby
{
    public bool? EndsWithSpace { get; set; }
    public List<TtmlRuby>? Ruby { get; set; }
    public bool? Obscene { get; set; }
    public uint? EmptyBeat { get; set; }
}
