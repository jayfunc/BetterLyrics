// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using System;
using System.IO;
using System.Text;
using Ude;

namespace BetterLyrics.WinUI3.Helper
{
    public class FileHelper
    {
        public static Encoding GetEncoding(string filename)
        {
            var bytes = File.ReadAllBytes(filename);
            var cdet = new CharsetDetector();
            cdet.Feed(bytes, 0, bytes.Length);
            cdet.DataEnd();
            var encoding = cdet.Charset;
            if (encoding == null)
            {
                return Encoding.UTF8;
            }
            return Encoding.GetEncoding(encoding);
        }

        public static string SanitizeFileName(string fileName, char replacement = '_')
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? replacement : c);
            }
            return sb.ToString();
        }

        public static string? ReadLyricsCache(string title, string artist, LyricsFormat format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {title}{format.ToFileExtension()}"));
            if (File.Exists(cacheFilePath))
            {
                return File.ReadAllText(cacheFilePath);
            }
            return null;
        }

        public static byte[]? ReadAlbumArtCache(string album, string artist, string format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {album}{format}"));
            if (File.Exists(cacheFilePath))
            {
                return File.ReadAllBytes(cacheFilePath);
            }
            return null;
        }

        public static void WriteLyricsCache(string title, string artist, string lyrics, LyricsFormat format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {title}{format.ToFileExtension()}"));
            File.WriteAllText(cacheFilePath, lyrics);
        }

        public static void WriteAlbumArtCache(string album, string artist, byte[] img, string format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {album}{format}"));
            File.WriteAllBytes(cacheFilePath, img);
        }

        public static bool IsSwitchableNormalizedMatch(string fileName, string q1, string q2)
        {
            var normFileName = fileName.Normalize();
            var normQ1 = q1.Normalize();
            var normQ2 = q2.Normalize();

            // 常见两种顺序
            return normFileName == normQ1 + normQ2
                || normFileName == normQ2 + normQ1;
        }

    }
}
