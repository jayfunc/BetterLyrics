namespace Lyricify.Lyrics.Models
{
    public class FileInfo
    {
        public LyricsTypes Type { get; set; }

        public SyncTypes SyncTypes { get; set; }

        public IAdditionalFileInfo? AdditionalInfo { get; set; }
    }
}
