// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    #region Interfaces

    /// <summary>
    /// Defines the <see cref="IMusicSearchService" />
    /// </summary>
    public interface IMusicSearchService
    {
        #region Methods

        /// <summary>
        /// The SearchAlbumArtAsync
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <returns>The <see cref="byte[]?"/></returns>
        byte[]? SearchAlbumArtAsync(string title, string artist);

        /// <summary>
        /// The SearchLyricsAsync
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="album">The album<see cref="string"/></param>
        /// <param name="durationMs">The durationMs<see cref="double"/></param>
        /// <param name="matchMode">The matchMode<see cref="MusicSearchMatchMode"/></param>
        /// <returns>The <see cref="Task{(string?, LyricsFormat?)}"/></returns>
        Task<(string?, LyricsFormat?)> SearchLyricsAsync(
            string title,
            string artist,
            string album = "",
            double durationMs = 0.0,
            MusicSearchMatchMode matchMode = MusicSearchMatchMode.TitleAndArtist
        );

        #endregion
    }

    #endregion
}
