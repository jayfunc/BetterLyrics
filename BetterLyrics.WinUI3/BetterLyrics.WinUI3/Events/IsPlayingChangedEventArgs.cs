// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Events
{
    /// <summary>
    /// Defines the <see cref="IsPlayingChangedEventArgs" />
    /// </summary>
    public class IsPlayingChangedEventArgs(bool isPlaying) : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether IsPlaying
        /// </summary>
        public bool IsPlaying { get; set; } = isPlaying;

        #endregion
    }
}
