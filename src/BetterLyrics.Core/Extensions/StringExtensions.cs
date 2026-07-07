using System.Net;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Extensions;

public static class StringExtensions
{
    private static readonly string[] _splitter =
    [
        ";",
        ",",
        "/",
        "；",
        "、",
        "，"
    ];

    extension(string str)
    {
        public string[] SplitByCommonSplitter()
        {
            var splitter = _splitter.FirstOrDefault(str.Contains);
            if (splitter != null) return str.Split(splitter);

            return [str];
        }

        public LyricsFormat? DetectFormat()
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;

            // TTML: 检查 <tt ... xmlns="http://www.w3.org/ns/ttml"
            if (Regex.IsMatch(
                    str,
                    @"<tt\b[^>]*\bxmlns\s*=\s*[""']http://www\.w3\.org/ns/ttml[""']",
                    RegexOptions.IgnoreCase))
                return LyricsFormat.Ttml;
            // KRC: 检测主内容格式 [start,duration]<offset,duration,0>字...
            if (Regex.IsMatch(
                    str,
                    @"^\[\d+,\d+\](<\d+,\d+,0>.+)+",
                    RegexOptions.Multiline))
                return LyricsFormat.Krc;
            // QRC: 检测主内容格式 [start,duration]字(offset,duration)
            if (Regex.IsMatch(
                    str,
                    @"^\[\d+,\d+\].*?\(\d+,\d+\)",
                    RegexOptions.Multiline))
                return LyricsFormat.Qrc;
            // 标准LRC和增强型LRC
            if (Regex.IsMatch(str, @"\[\d{1,2}:\d{2}") ||
                Regex.IsMatch(str, @"<\d{1,2}:\d{2}\.\d{2,3}>"))
                return LyricsFormat.Lrc;

            return null;
        }

        public string ToDecodedAbsoluteUri()
        {
            if (string.IsNullOrEmpty(str)) return "";
            try
            {
                var u = new Uri(str);
                return u.IsFile ? u.LocalPath : WebUtility.UrlDecode(u.AbsoluteUri);
            }
            catch
            {
                return str;
            }
        }
    }
}