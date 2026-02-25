namespace Lyricify.Lyrics.Decrypter.Qrc
{
    public class Helper
    {
        /// <summary>
        /// 通过 Mid 获取解密后的歌词
        /// </summary>
        /// <param name="id">QQ 音乐歌曲 Mid</param>
        /// <returns></returns>
        public static QqLyricsResponse? GetLyricsByMid(string mid)
        {
            var song = Providers.Web.Providers.QQMusicApi.GetSong(mid).Result;
            if (song == null || song.Data is not { Length: > 0 }) return null;
            var id = song.Data?[0].Id;
            return Providers.Web.Providers.QQMusicApi.GetLyricsAsync(id!).Result;
        }

        /// <summary>
        /// 通过 Mid 获取解密后的歌词
        /// </summary>
        /// <param name="id">QQ 音乐歌曲 Mid</param>
        /// <returns></returns>
        public static async Task<QqLyricsResponse?> GetLyricsByMidAsync(string mid)
        {
            var song = await Providers.Web.Providers.QQMusicApi.GetSong(mid);
            if (song == null || song.Data is not { Length: > 0 }) return null;
            var id = song.Data?[0].Id;
            return await Providers.Web.Providers.QQMusicApi.GetLyricsAsync(id!);
        }

        /// <summary>
        /// 通过 ID 获取解密后的歌词
        /// </summary>
        /// <param name="id">QQ 音乐歌曲 ID</param>
        /// <returns></returns>
        public static QqLyricsResponse? GetLyrics(string id)
        {
            return Providers.Web.Providers.QQMusicApi.GetLyricsAsync(id).Result;
        }

        /// <summary>
        /// 通过 ID 获取解密后的歌词
        /// </summary>
        /// <param name="id">QQ 音乐歌曲 ID</param>
        /// <returns></returns>
        public static async Task<QqLyricsResponse?> GetLyricsAsync(string id)
        {
            return await Providers.Web.Providers.QQMusicApi.GetLyricsAsync(id);
        }
    }
}
