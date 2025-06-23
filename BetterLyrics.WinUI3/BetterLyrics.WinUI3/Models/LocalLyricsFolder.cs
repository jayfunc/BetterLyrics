// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="LocalLyricsFolder" />
    /// </summary>
    public partial class LocalLyricsFolder : ObservableObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalLyricsFolder"/> class.
        /// </summary>
        public LocalLyricsFolder()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalLyricsFolder"/> class.
        /// </summary>
        /// <param name="path">The path<see cref="string"/></param>
        /// <param name="isEnabled">The isEnabled<see cref="bool"/></param>
        public LocalLyricsFolder(string path, bool isEnabled)
        {
            Path = path;
            IsEnabled = isEnabled;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether IsEnabled
        /// </summary>
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the Path
        /// </summary>
        [ObservableProperty]
        public partial string Path { get; set; }

        #endregion
    }
}
