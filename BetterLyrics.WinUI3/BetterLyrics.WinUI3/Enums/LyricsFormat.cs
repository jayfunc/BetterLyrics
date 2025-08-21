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
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // TTML: 检查 <tt ... xmlns="http://www.w3.org/ns/ttml"
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    content,
                    @"<tt\b[^>]*\bxmlns\s*=\s*[""']http://www\.w3\.org/ns/ttml[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return LyricsFormat.Ttml;
            }
            // KRC: 检测主内容格式 [start,duration]<offset,duration,0>字...
            else if (System.Text.RegularExpressions.Regex.IsMatch(
                         content,
                         @"^\[\d+,\d+\](<\d+,\d+,0>.+)+",
                         System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                return LyricsFormat.Krc;
            }
            // QRC: 检测主内容格式 [start,duration]字(offset,duration)
            else if (System.Text.RegularExpressions.Regex.IsMatch(
                         content,
                         @"^\[\d+,\d+\].*?\(\d+,\d+\)",
                         System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                return LyricsFormat.Qrc;
            }
            // 标准LRC和增强型LRC
            else if (System.Text.RegularExpressions.Regex.IsMatch(content, @"\[\d{1,2}:\d{2}") ||
                     System.Text.RegularExpressions.Regex.IsMatch(content, @"<\d{1,2}:\d{2}\.\d{2,3}>"))
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

        public static LyricsSearchProvider? ToLyricsSearchProvider(this LyricsFormat format)
        {
            return format switch
            {
                LyricsFormat.Lrc => LyricsSearchProvider.LocalLrcFile,
                LyricsFormat.Eslrc => LyricsSearchProvider.LocalEslrcFile,
                LyricsFormat.Ttml => LyricsSearchProvider.LocalTtmlFile,
                _ => null,
            };
        }
    }
}
