using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Services
{
    public interface IMusicSearchService
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="title"></param>
        /// <param name="artist"></param>
        /// <param name="album"></param>
        /// <param name="durationMs"></param>
        /// <param name="matchMode"></param>
        /// <param name="targetProps"></param>
        /// <param name="searchProviders"></param>
        /// <returns>Return a tuple (raw lyrics, lyrics format, album art)</returns>
        Task<(string?, LyricsFormat?)> SearchLyricsAsync(
            string title,
            string artist,
            string album = "",
            double durationMs = 0.0,
            MusicSearchMatchMode matchMode = MusicSearchMatchMode.TitleAndArtist
        );
    }
}
