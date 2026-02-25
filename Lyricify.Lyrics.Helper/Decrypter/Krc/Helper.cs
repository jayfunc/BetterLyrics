using Newtonsoft.Json;

namespace Lyricify.Lyrics.Decrypter.Krc
{
    public class Helper
    {
        public readonly static HttpClient Client = new();

        /// <summary>
        /// 通过 ID 和 AccessKey 获取解密后的歌词
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        public static string? GetLyrics(string id, string accessKey)
        {
            var encryptedLyrics = GetEncryptedLyrics(id, accessKey);
            var lyrics = Decrypter.DecryptLyrics(encryptedLyrics!);
            return lyrics;
        }

        /// <summary>
        /// 通过 ID 和 AccessKey 获取加密的歌词
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        public static string? GetEncryptedLyrics(string id, string accessKey)
        {
            var json = Client.GetStringAsync($"https://lyrics.kugou.com/download?ver=1&client=pc&id={id}&accesskey={accessKey}&fmt=krc&charset=utf8").Result;
            try
            {
                var response = JsonConvert.DeserializeObject<KugouLyricsResponse>(json);
                return response?.Content;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 通过 ID 和 AccessKey 获取解密后的歌词
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        public static async Task<string?> GetLyricsAsync(string id, string accessKey)
        {
            var encryptedLyrics = await GetEncryptedLyricsAsync(id, accessKey);
            var lyrics = Decrypter.DecryptLyrics(encryptedLyrics!);
            return lyrics;
        }

        /// <summary>
        /// 通过 ID 和 AccessKey 获取加密的歌词
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        public static async Task<string?> GetEncryptedLyricsAsync(string id, string accessKey)
        {
            var json = await Client.GetStringAsync($"https://lyrics.kugou.com/download?ver=1&client=pc&id={id}&accesskey={accessKey}&fmt=krc&charset=utf8");
            try
            {
                var response = JsonConvert.DeserializeObject<KugouLyricsResponse>(json);
                return response?.Content;
            }
            catch
            {
                return null;
            }
        }
    }
}
