// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="SongInfo" />
    /// </summary>
    public partial class SongInfo : ObservableObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SongInfo"/> class.
        /// </summary>
        public SongInfo()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Album
        /// </summary>
        [ObservableProperty]
        public partial string? Album { get; set; }

        /// <summary>
        /// Gets or sets the AlbumArt
        /// </summary>
        public byte[]? AlbumArt { get; set; } = null;

        /// <summary>
        /// Gets or sets the Artist
        /// </summary>
        [ObservableProperty]
        public partial string Artist { get; set; }

        /// <summary>
        /// Gets or sets the DurationMs
        /// In milliseconds
        /// </summary>
        [ObservableProperty]
        public partial double? DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the SourceAppUserModelId
        /// </summary>
        [ObservableProperty]
        public partial string? SourceAppUserModelId { get; set; } = null;

        /// <summary>
        /// Gets or sets the Title
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; }

        #endregion
    }
}
