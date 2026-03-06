namespace Lyricify.Lyrics.Models
{
    /// <summary>
    /// Lyrics type enumerates
    /// </summary>
    public enum SyncTypes
    {
        /// <summary>
        /// Sync type unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Lyrics lines are syllable synced
        /// </summary>
        SyllableSynced = 1,

        /// <summary>
        /// Lyrics are line synced
        /// </summary>
        LineSynced = 2,

        /// <summary>
        /// Lyrics line are mixed synced (contains both line and syllable sync)
        /// </summary>
        MixedSynced = 3,

        /// <summary>
        /// Lyrics are not synced
        /// </summary>
        Unsynced = 4,
    }
}
