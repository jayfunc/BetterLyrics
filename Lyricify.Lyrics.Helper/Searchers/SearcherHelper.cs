namespace Lyricify.Lyrics.Searchers
{
    /// <summary>
    /// 实例化搜索提供者的静态类
    /// </summary>
    public static class SearcherHelper
    {
        private static QQMusicSearcher? _qqMusicSearcher;

        public static QQMusicSearcher QQMusicSearcher => _qqMusicSearcher ??= new();

        private static NeteaseSearcher? _neteaseSearcher;

        public static NeteaseSearcher NeteaseSearcher => _neteaseSearcher ??= new();

        private static KugouSearcher? _kugouSearcher;

        public static KugouSearcher KugouSearcher => _kugouSearcher ??= new();

        private static MusixmatchSearcher? _musixmatchSearcher;

        public static MusixmatchSearcher MusixmatchSearcher => _musixmatchSearcher ??= new();

        private static SodaMusicSearcher? _sodaMusicSearcher;

        public static SodaMusicSearcher SodaMusicSearcher => _sodaMusicSearcher ??= new();

        private static AppleMusicSearcher? _appleMusicSearcher;

        public static AppleMusicSearcher AppleMusicSearcher => _appleMusicSearcher ??= new();
    }
}
