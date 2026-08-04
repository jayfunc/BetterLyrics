// 2025/6/23 by Zhe Fang

using System.Text;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models;
using Ude;

namespace BetterLyrics.Core.Helpers;

public class FileHelper
{
    public static readonly string[] MusicExtensions =
    {
        ".mp3", ".aac", ".m4a", ".ogg", ".opus", ".wma", ".amr",
        ".flac", ".alac", ".ape", ".wv", ".tak",
        ".wav", ".aiff", ".aif", ".pcm", ".cda", ".dsf", ".dff", ".au", ".snd",
        ".mid", ".midi", ".mod", ".xm", ".it", ".s3m"
    };

    public static readonly string[] LyricExtensions =
        Enum.GetValues(typeof(LyricsProvider)).Cast<LyricsProvider>()
            .Where(x => x.IsLocal())
            .Select(x => x.GetLyricsFormat())
            .Where(x => x != LyricsFormat.NotSpecified)
            .Select(x => x.ToFileExtension())
            .ToArray();

    public static readonly HashSet<string> AllSupportedExtensions = new(MusicExtensions.Union(LyricExtensions));

    public static Encoding GetEncoding(string filename)
    {
        var bytes = File.ReadAllBytes(filename);
        var cdet = new CharsetDetector();
        cdet.Feed(bytes, 0, bytes.Length);
        cdet.DataEnd();
        var encoding = cdet.Charset;
        if (encoding == null) return Encoding.UTF8;
        return Encoding.GetEncoding(encoding);
    }

    public static async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var destinationStream =
               new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }
    }

    public static string SanitizeFileName(string fileName, char replacement = '_')
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(fileName.Length);
        foreach (var c in fileName) sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? replacement : c);
        return sb.ToString();
    }

    public static byte[]? ReadAlbumArtCache(SongInfo songInfo, string format, string cacheFolderPath)
    {
        var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{songInfo.ToSearchString()}{format}"));
        if (File.Exists(cacheFilePath)) return File.ReadAllBytes(cacheFilePath);
        return null;
    }

    public static void WriteAlbumArtCache(SongInfo songInfo, byte[] img, string format, string cacheFolderPath)
    {
        var cacheFilePath = Path.Combine(cacheFolderPath, SanitizeFileName($"{songInfo.ToSearchString()}{format}"));
        File.WriteAllBytes(cacheFilePath, img);
    }
}