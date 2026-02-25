using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Text;

namespace Lyricify.Lyrics.Decrypter.Krc
{
    public class Decrypter
    {
        protected readonly static byte[] DecryptKey = { 0x40, 0x47, 0x61, 0x77, 0x5e, 0x32, 0x74, 0x47, 0x51, 0x36, 0x31, 0x2d, 0xce, 0xd2, 0x6e, 0x69 };

        /// <summary>
        /// 解密 KRC 歌词
        /// </summary>
        /// <param name="encryptedLyrics">加密的歌词</param>
        /// <returns>解密后的 KRC 歌词</returns>
        public static string? DecryptLyrics(string encryptedLyrics)
        {
            var data = Convert.FromBase64String(encryptedLyrics)[4..];

            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = (byte)(data[i] ^ DecryptKey[i % DecryptKey.Length]);
            }

            var res = Encoding.UTF8.GetString(SharpZipLibDecompress(data));
            return res[1..];
        }

        protected static byte[] SharpZipLibDecompress(byte[] data)
        {
            var compressed = new MemoryStream(data);
            var decompressed = new MemoryStream();
            var inputStream = new InflaterInputStream(compressed);

            inputStream.CopyTo(decompressed);

            return decompressed.ToArray();
        }
    }
}
