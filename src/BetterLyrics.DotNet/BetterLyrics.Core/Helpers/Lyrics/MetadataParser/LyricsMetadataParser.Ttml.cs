using BetterLyrics.Core.Helpers.Lyrics.Ttml;
using BetterLyrics.Core.Models.Lyrics;
using System.Xml;

namespace BetterLyrics.Core.Helpers.Lyrics.MetadataParser;

public partial class LyricsMetadataParser
{
    private static LyricsMetadata ParseTtml(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new();
        try
        {
            return TtmlLyricsAdapter.ToMetadata(TtmlParser.ParseMetadata(TtmlParser.Load(content)));
        }
        catch (Exception ex) when (ex is XmlException or FormatException or OverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"TTML metadata parsing failed: {ex.Message}");
            return new();
        }
    }
}
