// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    #region Interfaces

    /// <summary>
    /// Defines the <see cref="ILibWatcherService" />
    /// </summary>
    public interface ILibWatcherService
    {
        #region Events

        /// <summary>
        /// Defines the MusicLibraryFilesChanged
        /// </summary>
        event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;

        #endregion

        #region Methods

        /// <summary>
        /// The UpdateWatchers
        /// </summary>
        /// <param name="folders">The folders<see cref="List{LocalLyricsFolder}"/></param>
        public void UpdateWatchers(List<LocalLyricsFolder> folders);

        #endregion
    }

    #endregion
}
