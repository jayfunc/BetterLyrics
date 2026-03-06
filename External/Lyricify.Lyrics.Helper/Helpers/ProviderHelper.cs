namespace Lyricify.Lyrics.Helpers
{
    /// <summary>
    /// 提供 API 实例化后的静态类
    /// </summary>
    public static class ProviderHelper
    {
        public static Providers.Web.QQMusic.Api QQMusicApi => Providers.Web.Providers.QQMusicApi;

        public static Providers.Web.Netease.Api NeteaseApi => Providers.Web.Providers.NeteaseApi;

        public static Providers.Web.Kugou.Api KugouApi => Providers.Web.Providers.KugouApi;

        public static Providers.Web.Musixmatch.Api MusixmatchApi => Providers.Web.Providers.MusixmatchApi;

        public static Providers.Web.SodaMusic.Api SodaMusicApi => Providers.Web.Providers.SodaMusicApi;

        public static Providers.Web.AppleMusic.Api AppleMusicApi => Providers.Web.Providers.AppleMusicApi;
    }
}
