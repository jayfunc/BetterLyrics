using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.MetadataParser;

public partial class LyricsMetadataParser
{
    private static LyricsMetadata ParseLrc(string content)
    {
        var metadata = new LyricsMetadata();

        string? line;
        var safetyCounter = 0;
        const int MAX_SCAN_LINES = 100;

        using (var reader = new StringReader(content))
        {
            while ((line = reader?.ReadLine()) != null && safetyCounter < MAX_SCAN_LINES)
            {
                safetyCounter++;
                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (IsTimeTag(line)) break;

                if (line.StartsWith("[") && line.EndsWith("]")) ParseTagLine(line, metadata);
            }
        }

        return metadata;
    }

    private static void ParseTagLine(string line, LyricsMetadata data)
    {
        var content = line.Substring(1, line.Length - 2);

        var colonIndex = content.IndexOf(':');

        if (colonIndex < 0) return;

        var key = content.Substring(0, colonIndex).ToLower().Trim();
        var value = content.Substring(colonIndex + 1).Trim();

        switch (key)
        {
            case "ti":
                data.Title = value;
                break;
            case "ar":
                data.Artist = value;
                break;
            case "al":
                data.Album = value;
                break;
            case "au":
                data.Author = value;
                break;
            case "lr":
                data.Lyricist = value;
                break;
            case "by":
                data.LrcCreator = value;
                break;
            case "length":
                data.Length = value;
                break;
            case "re":
            case "tool":
                data.Tool = value;
                break;
            case "ve":
                data.Version = value;
                break;
            case "#":
                data.Comments.Add(value);
                break;
            case "offset":
                if (int.TryParse(value, out var offsetVal)) data.Offset = offsetVal;
                break;
        }
    }

    private static bool IsTimeTag(string line)
    {
        return line.Length > 2 && line[0] == '[' && char.IsDigit(line[1]);
    }
}