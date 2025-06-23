// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using System;

namespace BetterLyrics.WinUI3.Services
{
    #region Interfaces

    /// <summary>
    /// Defines the <see cref="IPlaybackService" />
    /// </summary>
    public interface IPlaybackService
    {
        #region Events

        /// <summary>
        /// Defines the IsPlayingChanged
        /// </summary>
        event EventHandler<IsPlayingChangedEventArgs>? IsPlayingChanged;

        /// <summary>
        /// Defines the PositionChanged
        /// </summary>
        event EventHandler<PositionChangedEventArgs>? PositionChanged;

        /// <summary>
        /// Defines the SongInfoChanged
        /// </summary>
        event EventHandler<SongInfoChangedEventArgs>? SongInfoChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether IsPlaying
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets the Position
        /// </summary>
        TimeSpan Position { get; }

        /// <summary>
        /// Gets the SongInfo
        /// </summary>
        SongInfo? SongInfo { get; }

        #endregion
    }

    #endregion
}
