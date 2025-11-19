// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Serialization;
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

        public static LyricsSearchResult? ReadLyricsCache(SongInfo songInfo, LyricsSearchProvider lyricsSearchProvider)
        {
            var cacheFilePath = Path.Combine(
                lyricsSearchProvider.GetCacheDirectory(),
                SanitizeFileName($"{songInfo.ToFileName()}.json"));

            if (File.Exists(cacheFilePath))
            {
                var json = File.ReadAllText(cacheFilePath);
                var data = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LyricsSearchResult);
                return data;
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

        public static void WriteLyricsCache(SongInfo songInfo, LyricsSearchResult lyricsSearchResult)
        {
            var cacheFilePath = Path.Combine(
                lyricsSearchResult.Provider.GetCacheDirectory(),
                SanitizeFileName($"{songInfo.ToFileName()}.json"));
            var json = System.Text.Json.JsonSerializer.Serialize(lyricsSearchResult, SourceGenerationContext.Default.LyricsSearchResult);
            File.WriteAllText(cacheFilePath, json);
        }

        public static void WriteAlbumArtCache(SongInfo songInfo, byte[] img, string format, string cacheFolderPath)
        {
            var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{songInfo.DisplayArtists} - {songInfo.Album}{format}"));
            File.WriteAllBytes(cacheFilePath, img);
        }

        public static readonly string[] MusicExtensions = {
            ".mp3", ".aac", ".m4a", ".ogg", ".opus", ".wma", ".amr",
            ".flac", ".alac", ".ape", ".wv", ".tak",
            ".wav", ".aiff", ".aif", ".pcm", ".cda", ".dsf", ".dff", ".au", ".snd",
            ".mid", ".midi", ".mod", ".xm", ".it", ".s3m"
        };
    }
}
