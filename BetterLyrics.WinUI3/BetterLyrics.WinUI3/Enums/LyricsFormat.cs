using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Enums
{
    public enum LyricsFormat
    {
        Lrc,
        DecryptedQrc,
        DecryptedKrc,
    }

    public static class LyricsFormatExtensions
    {
        public static string ToFileExtension(this LyricsFormat format)
        {
            return format switch
            {
                LyricsFormat.Lrc => ".lrc",
                LyricsFormat.DecryptedQrc => ".decryptedqrc",
                LyricsFormat.DecryptedKrc => ".decryptedkrc",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
            };
        }

        public static List<string> GetSupportedLyricsFormatAsList()
        {
            return [.. Enum.GetValues<LyricsFormat>().Select(format => format.ToFileExtension())];
        }

        public static LyricsFormat FromFileExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".lrc" => LyricsFormat.Lrc,
                ".qrc" => LyricsFormat.DecryptedQrc,
                ".krc" => LyricsFormat.DecryptedKrc,
                _ => throw new ArgumentException(
                    $"Unsupported lyrics format: {extension}",
                    nameof(extension)
                ),
            };
        }
    }
}
