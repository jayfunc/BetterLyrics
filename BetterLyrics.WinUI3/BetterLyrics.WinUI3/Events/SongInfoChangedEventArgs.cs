// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    /// <summary>
    /// Defines the <see cref="SongInfoChangedEventArgs" />
    /// </summary>
    public class SongInfoChangedEventArgs(SongInfo? songInfo) : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SongInfo
        /// </summary>
        public SongInfo? SongInfo { get; set; } = songInfo;

        #endregion
    }
}
