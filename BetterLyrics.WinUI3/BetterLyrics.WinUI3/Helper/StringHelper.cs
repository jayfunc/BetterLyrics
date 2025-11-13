using System.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public static class StringHelper
    {
        // 去除空格、括号、下划线、横杠、点、大小写等
        public static string Normalize(string s) =>
            new string(s
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray())
                .ToLowerInvariant();
        public static string NewLine = "\n";
    }
}
