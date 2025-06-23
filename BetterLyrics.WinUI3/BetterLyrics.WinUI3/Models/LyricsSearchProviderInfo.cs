// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="LyricsSearchProviderInfo" />
    /// </summary>
    public partial class LyricsSearchProviderInfo : ObservableObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsSearchProviderInfo"/> class.
        /// </summary>
        public LyricsSearchProviderInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsSearchProviderInfo"/> class.
        /// </summary>
        /// <param name="provider">The provider<see cref="LyricsSearchProvider"/></param>
        /// <param name="isEnabled">The isEnabled<see cref="bool"/></param>
        public LyricsSearchProviderInfo(LyricsSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
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
        /// Gets or sets the Provider
        /// </summary>
        [ObservableProperty]
        public partial LyricsSearchProvider Provider { get; set; }

        #endregion
    }
}
