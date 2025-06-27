// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Events
{
    /// <summary>
    /// Defines the <see cref="PositionChangedEventArgs" />
    /// </summary>
    public class PositionChangedEventArgs(TimeSpan position) : EventArgs()
    {
        #region Properties

        /// <summary>
        /// Gets or sets the Position
        /// </summary>
        public TimeSpan Position { get; set; } = position;

        #endregion
    }
}
