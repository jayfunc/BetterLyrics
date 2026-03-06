namespace Lyricify.Lyrics.Providers.Web
{
    internal static class Providers
    {
        private static QQMusic.Api? _qqMusicApi;

        public static QQMusic.Api QQMusicApi => _qqMusicApi ??= new();

        private static Netease.Api? _neteaseApi;

        public static Netease.Api NeteaseApi => _neteaseApi ??= new();

        private static Kugou.Api? _kugouApi;

        public static Kugou.Api KugouApi => _kugouApi ??= new();

        private static Musixmatch.Api? _musixmatchApi;

        public static Musixmatch.Api MusixmatchApi => _musixmatchApi ??= new();

        private static SodaMusic.Api? _sodaMusicApi;

        public static SodaMusic.Api SodaMusicApi => _sodaMusicApi ??= new();

        private static AppleMusic.Api? _appleMusicApi;

        public static AppleMusic.Api AppleMusicApi => _appleMusicApi ??= new();
    }
}
