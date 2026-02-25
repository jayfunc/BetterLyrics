namespace Lyricify.Lyrics.Models
{
    public class LyricsData
    {
        public FileInfo? File { get; set; }

        public List<ILineInfo>? Lines { get; set; }

        public List<string>? Writers { get; set; }

        public ITrackMetadata? TrackMetadata { get; set; }
    }
}
