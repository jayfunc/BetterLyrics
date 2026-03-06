namespace Lyricify.Lyrics.Models
{
    public interface ITrackMetadata
    {
        /// <summary>
        /// Title of the track
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Artist(s) of the track
        /// </summary>
        public string? Artist { get; set; }

        /// <summary>
        /// Album name of the track
        /// </summary>
        public string? Album { get; set; }

        /// <summary>
        /// Artist(s) of the album
        /// </summary>
        public string? AlbumArtist { get; set; }

        /// <summary>
        /// Track length in milliseconds
        /// </summary>
        public int? DurationMs { get; set; }

        /// <summary>
        /// ISRC of the track
        /// </summary>
        public string? Isrc { get; set; }

        /// <summary>
        /// Languages of the lyrics
        /// </summary>
        public List<string>? Language { get; set; }
    }
}
