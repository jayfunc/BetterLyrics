using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Interfaces.Plugins
{
    public interface ILyricsSource
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="artist"></param>
        /// <param name="album"></param>
        /// <param name="duration"></param>
        /// <param name="token"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration, CancellationToken token);
    }
}
