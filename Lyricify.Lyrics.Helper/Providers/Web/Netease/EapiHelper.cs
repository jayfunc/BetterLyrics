using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

#nullable disable
namespace Lyricify.Lyrics.Providers.Web.Netease
{
    internal class EapiHelper
    {
        public static async Task<string> PostAsync(string url, HttpClient httpClient, Dictionary<string, string> data)
        {
            var headers = new Dictionary<string, string>
            {
                ["User-Agent"] = userAgent,
                ["Referer"] = "https://music.163.com/",
            };
            var header = new Dictionary<string, string>()
            {
                ["__csrf"] = "",
                ["appver"] = "8.0.0",
                ["buildver"] = GetCurrentTotalSeconds().ToString(),
                ["channel"] = string.Empty,
                ["deviceId"] = "",
                ["mobilename"] = string.Empty,
                ["resolution"] = "1920x1080",
                ["os"] = "android",
                ["osver"] = "",
                ["requestId"] = $"{GetCurrentTotalMilliseconds()}_{Math.Floor(new Random().NextDouble() * 1000).ToString().PadLeft(4, '0')}",
                ["versioncode"] = "140",
                ["MUSIC_U"] = "",
            };
            headers["Cookie"] = string.Join("; ", header.Select(t => t.Key + "=" + t.Value));
            data["header"] = JsonConvert.SerializeObject(header);
            var data2 = EApi(url, data);
            url = Regex.Replace(url, @"\w*api", "eapi");

            httpClient.DefaultRequestHeaders.Clear();
            foreach (var h in headers)
            {
                httpClient.DefaultRequestHeaders.Add(h.Key, h.Value);
            }
            using var response = await httpClient.PostAsync(url, new FormUrlEncodedContent(data2));
            response.EnsureSuccessStatusCode();
            byte[] buffer = await response.Content.ReadAsByteArrayAsync();
            return Encoding.UTF8.GetString(buffer);
        }

        private static ulong GetCurrentTotalSeconds()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)timeSpan.TotalSeconds;
        }

        private static ulong GetCurrentTotalMilliseconds()
        {
            var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)timeSpan.TotalMilliseconds;
        }

        private static readonly string userAgent = "Mozilla/5.0 (Linux; Android 9; PCT-AL10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.64 HuaweiBrowser/10.0.3.311 Mobile Safari/537.36";

        private static readonly byte[] eapiKey = Encoding.ASCII.GetBytes("e82ckenh8dichen8");

        public static Dictionary<string, string> EApi(string url, object @object)
        {
            url = url.Replace("https://interface3.music.163.com/e", "/");
            url = url.Replace("https://interface.music.163.com/e", "/");
            string text = JsonConvert.SerializeObject(@object);
            string message = $"nobody{url}use{text}md5forencrypt";
            string digest = message.ToByteArrayUtf8().ComputeMd5().ToHexStringLower();
            string data = $"{url}-36cd479b6b5-{text}-36cd479b6b5-{digest}";
            return new Dictionary<string, string>
            {
                ["params"] = AesEncrypt(data.ToByteArrayUtf8(), CipherMode.ECB, eapiKey, null).ToHexStringUpper()
            };
        }

        public static byte[] Decrypt(byte[] cipherBuffer)
        {
            return AesDecrypt(cipherBuffer, CipherMode.ECB, eapiKey, null);
        }

        private static byte[] AesEncrypt(byte[] buffer, CipherMode mode, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.BlockSize = 128;
            aes.Key = key;
            if (iv is not null)
                aes.IV = iv;
            aes.Mode = mode;
            using var cryptoTransform = aes.CreateEncryptor();
            return cryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
        }

        private static byte[] AesDecrypt(byte[] buffer, CipherMode mode, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.BlockSize = 128;
            aes.Key = key;
            if (iv is not null)
                aes.IV = iv;
            aes.Mode = mode;
            using var cryptoTransform = aes.CreateDecryptor();
            return cryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
        }

    }
    internal static class Extensions
    {
        public static byte[] ToByteArrayUtf8(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static string ToHexStringLower(this byte[] value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
                sb.Append(value[i].ToString("x2"));
            return sb.ToString();
        }

        public static string ToHexStringUpper(this byte[] value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
                sb.Append(value[i].ToString("X2"));
            return sb.ToString();
        }

        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        public static byte[] ComputeMd5(this byte[] value)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(value);
        }

        public static byte[] RandomBytes(this Random random, int length)
        {
            byte[] buffer = new byte[length];
            random.NextBytes(buffer);
            return buffer;
        }

        public static string Get(this CookieCollection cookies, string name, string defaultValue)
        {
            return cookies[name]?.Value ?? defaultValue;
        }
    }
}
