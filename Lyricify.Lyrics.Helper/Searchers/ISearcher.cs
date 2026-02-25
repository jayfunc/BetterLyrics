using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers.Helpers;

namespace Lyricify.Lyrics.Searchers
{
    /// <summary>
    /// 搜索提供者接口
    /// </summary>
    public interface ISearcher
    {
        /// <summary>
        /// 搜索源名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 搜索源显示名称 (in English)
        /// </summary>
        public string DisplayName { get; }

        public Searchers SearcherType { get; }

        /// <summary>
        /// 搜索最佳匹配的曲目
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public Task<ISearchResult?> SearchForResult(ITrackMetadata track);

        /// <summary>
        /// 搜索最佳匹配的曲目
        /// </summary>
        /// <param name="track"></param>
        /// <param name="minimumMatch">最低匹配要求</param>
        /// <returns></returns>
        public Task<ISearchResult?> SearchForResult(ITrackMetadata track, CompareHelper.MatchType minimumMatch);

        /// <summary>
        /// 搜索匹配的曲目列表
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public Task<List<ISearchResult>> SearchForResults(ITrackMetadata track);

        /// <summary>
        /// 搜索匹配的曲目列表
        /// </summary>
        /// <param name="track"></param>
        /// <param name="fullSearch">是否是完整搜索</param>
        /// <returns></returns>
        public Task<List<ISearchResult>> SearchForResults(ITrackMetadata track, bool fullSearch);

        /// <summary>
        /// 搜索关键字的曲目列表
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public Task<List<ISearchResult>?> SearchForResults(string searchString);
    }
}
