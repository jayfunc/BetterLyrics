using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.MetadataParser
{
    public partial class LyricsMetadataParser
    {
        public static LyricsMetadata Parse(string content, string ext)
        {
            LyricsMetadata metadata = new();
            if (ext == ".ttml")
            {
                metadata = LyricsMetadataParser.ParseTtml(content);
            }
            else
            {
                metadata = LyricsMetadataParser.ParseLrc(content);
            }
            return metadata;
        }
    }
}
