namespace Lyricify.Lyrics.Searchers
{
    /// <summary>
    /// 搜索提供者的静态帮助类
    /// </summary>
    public static class SearchersHelper
    {
        /// <summary>
        /// 获取枚举的对应类实例
        /// </summary>
        /// <param name="searcher"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static ISearcher GetSearcher(this Searchers searcher)
        {
            return searcher switch
            {
                Searchers.QQMusic => SearcherHelper.QQMusicSearcher,
                Searchers.Netease => SearcherHelper.NeteaseSearcher,
                Searchers.Kugou => SearcherHelper.KugouSearcher,
                Searchers.Musixmatch => SearcherHelper.MusixmatchSearcher,
                Searchers.SodaMusic => SearcherHelper.SodaMusicSearcher,
                Searchers.AppleMusic => SearcherHelper.AppleMusicSearcher,
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// 获取枚举的对应类新实例
        /// </summary>
        /// <param name="searcher"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static ISearcher GetNewSearcher(this Searchers searcher)
        {
            return searcher switch
            {
                Searchers.QQMusic => new QQMusicSearcher(),
                Searchers.Netease => new NeteaseSearcher(),
                Searchers.Kugou => new KugouSearcher(),
                Searchers.Musixmatch => new MusixmatchSearcher(),
                Searchers.SodaMusic => new SodaMusicSearcher(),
                Searchers.AppleMusic => new AppleMusicSearcher(),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// 获取搜索类的对应枚举
        /// </summary>
        /// <param name="searcher"></param>
        /// <returns></returns>
        public static Searchers? GetSearchers(this ISearcher searcher)
        {
            if (searcher is QQMusicSearcher) return Searchers.QQMusic;
            if (searcher is NeteaseSearcher) return Searchers.Netease;
            if (searcher is KugouSearcher) return Searchers.Kugou;
            if (searcher is MusixmatchSearcher) return Searchers.Musixmatch;
            if (searcher is SodaMusicSearcher) return Searchers.SodaMusic;
            if (searcher is AppleMusicSearcher) return Searchers.AppleMusic;
            return null;
        }
    }
}
