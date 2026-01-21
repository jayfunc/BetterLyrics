using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Parsers.LyricsMetadataParser
{
    public partial class LyricsMetadataParser
    {
        public static LyricsMetadata Parse(string content, string ext)
        {
            LyricsMetadata metadata = new();
            if (ext == ".ttml")
            {
                metadata = ParseTtml(content);
            }
            else
            {
                metadata = ParseLrc(content);
            }
            return metadata;
        }
    }
}
