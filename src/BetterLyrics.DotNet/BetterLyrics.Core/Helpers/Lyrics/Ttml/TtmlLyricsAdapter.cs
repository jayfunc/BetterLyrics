using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.Ttml;

internal static class TtmlLyricsAdapter
{
    internal static LyricsLine ToLine(TtmlContent content, uint start, uint end, string? agent, bool spaceJoined = false)
    {
        var line = new LyricsLine
        {
            PrimaryText = content.Text, StartMs = checked((int)start), EndMs = checked((int)end),
            AgentId = agent ?? "", IsPrimaryHasRealSyllableInfo = content.Words is { Count: > 0 }
        };
        var index = 0;
        foreach (var word in content.Words ?? [])
        {
            var text = word.Text;
            if (index + text.Length < content.Text.Length && (spaceJoined || word.EndsWithSpace == true)) text += " ";
            line.PrimarySyllables.Add(new BaseLyrics
            {
                Text = text, StartIndex = index, StartMs = checked((int)word.StartTime), EndMs = checked((int)word.EndTime)
            });
            index += text.Length;
        }
        return line;
    }

    internal static LyricsMetadata ToMetadata(TtmlMetadata source)
    {
        var writers = string.Join("/", (source.Songwriters ?? []).Distinct());
        var duration = source.Audio?.Duration;
        return new LyricsMetadata
        {
            Title = source.Title?.FirstOrDefault() ?? "",
            Artist = string.Join("/", (source.Artist ?? source.Agents?.Values.Select(a => a.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList() ?? []).Distinct()),
            Album = source.Album?.FirstOrDefault() ?? "",
            Author = writers, Lyricist = writers,
            Offset = source.Audio?.LyricOffset ?? 0,
            LrcCreator = string.Join("/", (source.AuthorNames ?? []).Concat(source.AuthorIds ?? []).Distinct()),
            Length = duration.HasValue
                ? FormattableString.Invariant($"{duration.Value / 60000}:{duration.Value % 60000 / 1000:00}.{duration.Value % 1000:000}")
                : "00:00"
        };
    }
}
