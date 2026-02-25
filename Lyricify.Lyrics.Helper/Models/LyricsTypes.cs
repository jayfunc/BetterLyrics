namespace Lyricify.Lyrics.Models
{
    /// <summary>
    /// Lyrics type enumerates
    /// </summary>
    public enum LyricsTypes
    {
        Unknown = 0,
        LyricifySyllable = 1,
        LyricifyLines = 2,
        Lrc = 3,
        Qrc = 4,
        Krc = 5,
        Yrc = 6,
        Ttml = 7,
        Spotify = 8,
        Musixmatch = 9,
    }

    /// <summary>
    /// Lyrics raw string type enumerates
    /// </summary>
    public enum LyricsRawTypes
    {
        /// <summary>
        /// Unknown lyrics
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Lyricify Syllable
        /// </summary>
        LyricifySyllable = 1,

        /// <summary>
        /// Lyricify Lines
        /// </summary>
        LyricifyLines = 2,

        /// <summary>
        /// Regular LRC
        /// </summary>
        Lrc = 3,

        /// <summary>
        /// Main QRC
        /// </summary>
        Qrc = 4,

        /// <summary>
        /// Raw QRC (in XML format)
        /// </summary>
        QrcFull = 10,

        /// <summary>
        /// Raw KRC
        /// </summary>
        Krc = 5,

        /// <summary>
        /// Main YRC
        /// </summary>
        Yrc = 6,

        /// <summary>
        /// Netease Cloud Music API raw JSON data (with translation etc)
        /// </summary>
        YrcFull = 11,

        /// <summary>
        /// TTML
        /// </summary>
        Ttml = 7,

        /// <summary>
        /// Apple Music API raw JSON data (with ID and more info)
        /// </summary>
        AppleJson = 12,

        /// <summary>
        /// Spotify Desktop Client API Color-Lyrics raw JSON data
        /// </summary>
        Spotify = 8,

        /// <summary>
        /// Musixmatch Desktop Client API raw JSON data
        /// </summary>
        Musixmatch = 9,
    }
}
