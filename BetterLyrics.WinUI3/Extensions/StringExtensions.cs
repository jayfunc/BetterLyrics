using BetterLyrics.WinUI3.Enums;
using System;
using System.Linq;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class StringExtensions
    {
        private static readonly string[] _splitter =
        [
            ";"
            ,
            ","
            ,
            "/"
            ,
            "；"
            ,
            "、"
            ,
            "，"
        ];

        extension(string str)
        {
            public string[] SplitByCommonSplitter()
            {
                var splitter = _splitter.FirstOrDefault(str.Contains);
                if (splitter != null)
                {
                    return str.Split(splitter);
                }
                else
                {
                    return [str];
                }
            }

            public LyricsFormat? DetectFormat()
            {
                if (string.IsNullOrWhiteSpace(str))
                    return null;

                // TTML: 检查 <tt ... xmlns="http://www.w3.org/ns/ttml"
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        str,
                        @"<tt\b[^>]*\bxmlns\s*=\s*[""']http://www\.w3\.org/ns/ttml[""']",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return LyricsFormat.Ttml;
                }
                // KRC: 检测主内容格式 [start,duration]<offset,duration,0>字...
                else if (System.Text.RegularExpressions.Regex.IsMatch(
                             str,
                             @"^\[\d+,\d+\](<\d+,\d+,0>.+)+",
                             System.Text.RegularExpressions.RegexOptions.Multiline))
                {
                    return LyricsFormat.Krc;
                }
                // QRC: 检测主内容格式 [start,duration]字(offset,duration)
                else if (System.Text.RegularExpressions.Regex.IsMatch(
                             str,
                             @"^\[\d+,\d+\].*?\(\d+,\d+\)",
                             System.Text.RegularExpressions.RegexOptions.Multiline))
                {
                    return LyricsFormat.Qrc;
                }
                // 标准LRC和增强型LRC
                else if (System.Text.RegularExpressions.Regex.IsMatch(str, @"\[\d{1,2}:\d{2}") ||
                         System.Text.RegularExpressions.Regex.IsMatch(str, @"<\d{1,2}:\d{2}\.\d{2,3}>"))
                {
                    return LyricsFormat.Lrc;
                }
                else
                {
                    return null;
                }
            }

            public string ToDecodedAbsoluteUri()
            {
                if (string.IsNullOrEmpty(str)) return "";
                try
                {
                    var u = new Uri(str);
                    return u.IsFile ? u.LocalPath : System.Net.WebUtility.UrlDecode(u.AbsoluteUri);
                }
                catch { return str; }
            }

        }
    }
}
