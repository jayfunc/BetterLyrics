using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Lyricify.Lyrics.Searchers.Helpers;

namespace Lyricify.Lyrics.Helpers
{
    /// <summary>
    /// 搜索帮助类
    /// </summary>
    public static class SearchHelper
    {
        /// <summary>
        /// 搜索指定曲目的对应曲目
        /// </summary>
        /// <param name="track">指定曲目</param>
        /// <param name="searcher">搜索提供者</param>
        /// <returns>对应曲目</returns>
        public static async Task<ISearchResult?> Search(ITrackMetadata track, Searchers.Searchers searcher)
            => await Search(track, searcher.GetSearcher());

        /// <summary>
        /// 搜索指定曲目的对应曲目
        /// </summary>
        /// <param name="track">指定曲目</param>
        /// <param name="searcher">搜索提供者</param>
        /// <param name="minimumMatch">最低匹配要求</param>
        /// <returns>对应曲目</returns>
        public static async Task<ISearchResult?> Search(ITrackMetadata track, Searchers.Searchers searcher, CompareHelper.MatchType minimumMatch)
            => await Search(track, searcher.GetSearcher(), minimumMatch);

        /// <summary>
        /// 搜索指定曲目的对应曲目
        /// </summary>
        /// <param name="track">指定曲目</param>
        /// <param name="searcher">搜索提供者</param>
        /// <returns>对应曲目</returns>
        public static async Task<ISearchResult?> Search(ITrackMetadata track, ISearcher searcher)
            => await searcher.SearchForResult(track);

        /// <summary>
        /// 搜索指定曲目的对应曲目
        /// </summary>
        /// <param name="track">指定曲目</param>
        /// <param name="searcher">搜索提供者</param>
        /// <param name="minimumMatch">最低匹配要求</param>
        /// <returns>对应曲目</returns>
        public static async Task<ISearchResult?> Search(ITrackMetadata track, ISearcher searcher, CompareHelper.MatchType minimumMatch)
            => await searcher.SearchForResult(track, minimumMatch);
    }
}