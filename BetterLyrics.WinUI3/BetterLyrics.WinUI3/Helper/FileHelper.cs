// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string? ReadLyricsCache(string title, string artist, string album, LyricsFormat format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {title} - {album}{format.ToFileExtension()}"));
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

        public static void WriteLyricsCache(string title, string artist, string album, string lyrics, LyricsFormat format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {title} - {album}{format.ToFileExtension()}"));
            File.WriteAllText(cacheFilePath, lyrics);
        }

        public static void WriteAlbumArtCache(string album, string artist, byte[] img, string format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{artist} - {album}{format}"));
            File.WriteAllBytes(cacheFilePath, img);
        }

        public static bool IsSwitchableNormalizedMatch(string fileName, string q1, string q2)
        {
            var normFileName = StringHelper.Normalize(fileName);
            var normQ1 = StringHelper.Normalize(q1);
            var normQ2 = StringHelper.Normalize(q2);

            // 常见两种顺序
            return normFileName == normQ1 + normQ2
                || normFileName == normQ2 + normQ1;
        }

        public static readonly string[] MusicExtensions = {
            ".mp3", ".aac", ".m4a", ".ogg", ".opus", ".wma", ".amr",
            ".flac", ".alac", ".ape", ".wv", ".tak",
            ".wav", ".aiff", ".aif", ".pcm", ".cda", ".dsf", ".dff", ".au", ".snd",
            ".mid", ".midi", ".mod", ".xm", ".it", ".s3m"
        };

        public static string MusicSearchPattern => string.Join("|", MusicExtensions.Select(x => $"*{x}"));
    }
}
