using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Helpers.Types
{
    public static class Lrc
    {
        public static bool IsLrc(string input)
        {
            if (input == null) return false;

            var pattern = @"\[\d+:\d+\.\d+\](.+)";
            var regex = new Regex(pattern);

            MatchCollection matches = regex.Matches(input);
            return matches.Count > 0;
        }
    }
}
