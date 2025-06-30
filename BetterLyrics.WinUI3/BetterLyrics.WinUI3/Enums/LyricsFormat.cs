// 2025/6/23 by Zhe Fang

namespace BetterLyrics.WinUI3.Enums
{
    public enum LyricsFormat
    {
        Lrc,
        Eslrc,
        Ttml,
        Qrc,
        Krc,
        NotSpecified,
    }

    public static class LyricsFormatExtensions
    {
        public static LyricsFormat? DetectFormat(this string content)
        {
            if (content.StartsWith("<?xml") && System.Text.RegularExpressions.Regex.IsMatch(content, @"<tt(:\w+)?\b"))
            {
                return LyricsFormat.Ttml;
            }
            // 检测标准LRC和增强型LRC
            else if (System.Text.RegularExpressions.Regex.IsMatch(content, @"\[\d{1,2}:\d{2}")
                || System.Text.RegularExpressions.Regex.IsMatch(content, @"<\d{1,2}:\d{2}\.\d{2,3}>"))
            {
                return LyricsFormat.Lrc;
            }
            else
            {
                return null;
            }
        }

        public static string ToFileExtension(this LyricsFormat format)
        {
            return format switch
            {
                LyricsFormat.Lrc => ".lrc",
                LyricsFormat.Qrc => ".qrc",
                LyricsFormat.Krc => ".krc",
                LyricsFormat.Eslrc => ".eslrc",
                LyricsFormat.Ttml => ".ttml",
                _ => ".*",
            };
        }
    }
}
